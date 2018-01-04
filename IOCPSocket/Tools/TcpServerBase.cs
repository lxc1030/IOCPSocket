using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Common
{
    public class TcpServerBase : TcpListener
    {
        /// <summary>
        /// 初始化tcp服务器。
        /// </summary>
        /// <param name="port">服务器端应用端口号。</param>
        /// <param name="bufferSize">为每一个客户端通道预留的buffer的大小。</param>
        /// <param name="asyncExecution">如果为true，在worker子线程（从系统线程池中分配）中触发OnMessage事件；如果为false，在I/O线程中触发。</param>
        public TcpServerBase(int port, int bufferSize = 8000, bool asyncExecution = true)
            : base(new IPEndPoint(IPAddress.Any, port))
        {
            this.BufferSize = bufferSize;
            this.AsyncExecution = asyncExecution;
            this.Start();
            this.BeginAcceptTcpClient(ClientConnected, null);
        }

        private void ClientConnected(IAsyncResult handler)
        {
            try
            {
                var client = this.EndAcceptTcpClient(handler);
                var stream = client.GetStream();
                if (OnOpen != null)
                    OnOpen((IPEndPoint)client.Client.RemoteEndPoint, stream);
                var buffer = new byte[BufferSize];
                var container = new List<byte>();
                stream.BeginRead(buffer, 0, buffer.Length, ProcessReceive, new object[] { stream, buffer, container });
            }
            finally
            {
                this.BeginAcceptTcpClient(ClientConnected, null);
            }
        }

        private void ProcessReceive(IAsyncResult h)
        {
            var objs = (object[])h.AsyncState;
            var stream = (NetworkStream)objs[0];
            var buffer = (byte[])objs[1];
            var container = (List<byte>)objs[2];
            ExecuteRead(stream, h, buffer, container, this.AsyncExecution, OnMessage, OnError);
            try
            {
                stream.BeginRead(buffer, 0, buffer.Length, ProcessReceive, objs);
            }
            catch (Exception ex)
            {
                if (OnError != null)
                    OnError(stream, ex);
            }
        }

        /// <summary>
        /// 读取对方传送的数据，分隔为一个或者多个命令行（以换行回车为结束符号），然后回调onMessage委托方法。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="h"></param>
        /// <param name="buffer"></param>
        /// <param name="container"></param>
        /// <param name="AsyncExecution"></param>
        /// <param name="onMessage"></param>
        /// <param name="onError"></param>
        internal static void ExecuteRead(NetworkStream stream, IAsyncResult h, byte[] buffer, List<byte> container,
            bool AsyncExecution, OnMessageEventHandler onMessage, OnErrorEventHandler onError)
        {
            try
            {
                var len = stream.EndRead(h);
                for (var i = 0; i < len; i++)
                    container.Add(buffer[i]);
                ExecuteCommand(stream, container, AsyncExecution, onMessage, onError);
            }
            catch (Exception ex)
            {
                if (onError != null)
                    onError(stream, ex);
            }
        }

        private static void ExecuteCommand(NetworkStream stream, List<byte> container, bool AsyncExecution,
            OnMessageEventHandler onMessage, OnErrorEventHandler onError)
        {
        begin:
            for (var i = 0; i < container.Count - 1; i++)
            {
                if (container[i] == 0x0d && container[i + 1] == 0x0a)
                {
                    if (onMessage != null)
                    {
                        var command = Encoding.UTF8.GetString(container.ToArray(), 0, i);
                        if (AsyncExecution)
                            ThreadPool.QueueUserWorkItem(h => ExecuteMessage(stream, command, onMessage, onError));
                        else
                            ExecuteMessage(stream, command, onMessage, onError);
                    }
                    container.RemoveRange(0, i + 2);
                    goto begin;
                }
            }
        }

        private static void ExecuteMessage(NetworkStream stream, string command,
           OnMessageEventHandler onMessage, OnErrorEventHandler onError)
        {
            try
            {
                onMessage(stream, command);
            }
            catch (Exception ex)
            {
                if (onError != null)
                    onError(stream, ex);
            }
        }

        /// <summary>
        /// 通过NetworkStream向对方发送一条消息。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="msg">要发送的命令。如果包含换行回车，应该替换为其它符号(例如仅有换行符)。如果含有二进制数据，应当将数据部分转为base64编码。</param>
        /// <param name="error"></param>
        public static void SendMessage(NetworkStream stream, string msg, Action<Exception> error = null)
        {
            if (msg.Contains("\r\n"))
            {
                error(new Exception("命令中不能包含换行回车。"));
                return;
            }

            var container = new List<byte>();
            container.AddRange(Encoding.UTF8.GetBytes(msg));
            container.Add(0x0d);
            container.Add(0x0a);
            var buffer = container.ToArray();
            stream.BeginWrite(buffer, 0, buffer.Length, x =>
            {
                try
                {
                    stream.EndWrite(x);
                }
                catch (Exception ex)
                {
                    if (error != null)
                        error(ex);
                }
            }, null);
        }

        public delegate void OnOpenEventHandler(IPEndPoint endpoint, NetworkStream stream);
        public delegate void OnCloseEventHandler(NetworkStream stream);
        public delegate void OnMessageEventHandler(NetworkStream stream, string message);
        public delegate void OnErrorEventHandler(NetworkStream stream, Exception ex);

        public event OnOpenEventHandler OnOpen;
        public event OnMessageEventHandler OnMessage;
        public event OnErrorEventHandler OnError;

        public int Port { get; private set; }
        public int BufferSize { get; private set; }
        public bool AsyncExecution { get; private set; }
    }
}
