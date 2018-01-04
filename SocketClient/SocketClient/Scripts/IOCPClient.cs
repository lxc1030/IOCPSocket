using CsharpIOCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    public class IOCPClient
    {
        /// <summary>  
        /// 连接服务器的socket  
        /// </summary>  
        private Socket _clientSock;

        /// <summary>  
        /// 用于服务器执行的互斥同步对象  
        /// </summary>  
        private static Mutex mutex = new Mutex();
        /// <summary>  
        /// Socket连接标志  
        /// </summary>  
        private Boolean _connected = false;

        private const int ReceiveOperation = 1, SendOperation = 0;

        private static AutoResetEvent[]
                 autoSendReceiveEvents = new AutoResetEvent[]
         {
            new AutoResetEvent(false),
            new AutoResetEvent(false)
         };

        /// <summary>  
        /// 服务器监听端点  
        /// </summary>  
        private IPEndPoint hostEndPoint;


        private AsyncUserToken MyUserToken;


        public IOCPClient(string IP, int portNo)
        {
            hostEndPoint = new IPEndPoint(IPAddress.Parse(IP), portNo);
            _clientSock = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Connect();
        }


        #region 连接服务器  

        /// <summary>  
        /// 连接远程服务器  
        /// </summary>  
        public void Connect()
        {
            MyUserToken = new AsyncUserToken();
            MyUserToken.S = _clientSock;

            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);

            connectArgs.UserToken = MyUserToken;
            connectArgs.RemoteEndPoint = hostEndPoint;


            SocketAsyncEventArgs saea_Send = new SocketAsyncEventArgs();
            saea_Send.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);

            MyUserToken.SAEA_Send = saea_Send;

            saea_Send.UserToken = MyUserToken;


            //mutex.WaitOne();
            if (!_clientSock.ConnectAsync(connectArgs))//异步连接  
            {
                ProcessConnected(connectArgs);
            }
        }
        /// <summary>
        /// 接收或发送完成异步操作回调
        /// </summary>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnected(e);
                    break;
                case SocketAsyncOperation.Receive:
                    //autoSendReceiveEvents[ReceiveOperation].Set();
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    //autoSendReceiveEvents[SendOperation].Set();
                    ProcessSend(e);
                    break;
            }
        }

        /// <summary>  
        /// 处理连接服务器  
        /// </summary>  
        /// <param name="e"></param>  
        private void ProcessConnected(SocketAsyncEventArgs e)
        {
            //TODO  
            if (e.SocketError == SocketError.Success)
            {
                Log4Debug("连接成功。");
                AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
                e.AcceptSocket = userToken.S;
                //Socket s = e.AcceptSocket;//和客户端关联的socket
                Socket s = userToken.S;
                if (s.Connected)
                {
                    try
                    {
                        byte[] receiveBuffer = new byte[1024];
                        e.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                        //autoSendReceiveEvents[ReceiveOperation].WaitOne();
                        if (!s.ReceiveAsync(e))
                        {
                            ProcessReceive(e);
                        }
                    }
                    catch (SocketException ex)
                    {
                        //TODO 异常处理
                    }
                }
            }
        }

        #endregion

        #region 发送消息  
        /// <summary>  
        /// 向服务器发送消息  
        /// </summary>  
        /// <param name="data"></param>  
        public void SendSave(byte[] data)
        {
            AsyncUserToken userToken = MyUserToken;
            lock (userToken.SendBuffer)
            {
                Log4Debug("保存数据:" + data[0]);
                for (int i = 0; i < data.Length; i++)
                {
                    userToken.SendBuffer.Enqueue(data[i]);
                }
            }

            Send(userToken.SAEA_Send);
        }
        public void Send(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;

            if (userToken.isSending)
            {
                Log4Debug("正在发送。");
                return;
            }
            userToken.isSending = true;

            byte[] buffer = null;
            lock (userToken.SendBuffer)
            {
                buffer = userToken.SendBuffer.ToArray();
                userToken.Clear(userToken.SendBuffer);
            }

            userToken.SAEA_Send.SetBuffer(buffer, 0, buffer.Length);
            Socket s = userToken.S;
            if (!s.SendAsync(userToken.SAEA_Send))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
            {
                // 同步发送时处理发送完成事件  
                ProcessSend(userToken.SAEA_Send);
            }
        }



        /// <summary>  
        /// 发送完成时处理函数  
        /// </summary>  
        /// <param name="e">与发送完成操作相关联的SocketAsyncEventArg对象</param>  
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            //TODO  
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
                userToken.isSending = false;
                if (userToken.SendBuffer.Count > 0)
                {
                    Send(e);
                }
                Log4Debug("发送成功。");
                //TODO
            }

        }
        #endregion

        #region 接收消息  

        /// <summary>  
        ///接收完成时处理函数  
        /// </summary>  
        /// <param name="e">与接收完成操作相关联的SocketAsyncEventArg对象</param>  
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
                    userToken.HeartbeatTime = DateTime.Now;
                    string sClientIP = ((IPEndPoint)userToken.S.RemoteEndPoint).Address.ToString();
                    try
                    {
                        byte[] abFactReceive = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, e.Offset, abFactReceive, 0, e.BytesTransferred);

                        string info = "";
                        for (int i = 0; i < abFactReceive.Length; i++)
                        {
                            info += abFactReceive[i] + ",";
                        }
                        //Console.WriteLine("From the " + sClientIP + " to receive " + e.BytesTransferred + " bytes of data：" + info);

                        lock (userToken.ReceiveBuffer)
                        {
                            for (int i = 0; i < abFactReceive.Length; i++)
                            {
                                userToken.ReceiveBuffer.Enqueue(abFactReceive[i]);
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine(error.Message);
                    }
                    finally
                    {
                        if (!userToken.S.ReceiveAsync(e))
                            ProcessReceive(e);
                    }
                }
                else
                {
                    //CloseClientSocket(e);
                }
            }
            catch { }
        }

        #endregion

        public void TestThread()
        {
            Thread t = new Thread(WriteY);
            t.Start();
        }
        void WriteY()
        {
            for (int i = 100; i < 110; i++)
            {
                byte j = (byte)i;
                SendSave(new byte[5] { j, j, j, j, j });
            }
        }

        public void DebugReceive()
        {
            string info = "长度：" + MyUserToken.ReceiveBuffer.Count + "/";
            byte[] rece = MyUserToken.ReceiveBuffer.ToArray();
            for (int i = 0; i < rece.Length; i++)
            {
                if (rece[i] == 10)
                {
                    break;
                }
                info += rece[i] + ",";
            }
            Console.WriteLine(info);
        }



        public void Close()
        {
            _clientSock.Disconnect(false);
        }

        /// <summary>  
        /// 失败时关闭Socket，根据SocketError抛出异常。  
        /// </summary>  
        /// <param name="e"></param>  

        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            if (s.Connected)
            {
                //关闭与客户端关联的Socket  
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    //如果客户端处理已经关闭，抛出异常   
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                }
            }
            //抛出SocketException   
            throw new SocketException((Int32)e.SocketError);
        }


        /// <summary>  
        /// 释放SocketClient实例  
        /// </summary>  
        public void Dispose()
        {
            mutex.Close();
            autoSendReceiveEvents[SendOperation].Close();
            autoSendReceiveEvents[ReceiveOperation].Close();
            if (_clientSock.Connected)
            {
                _clientSock.Close();
            }
        }
        public void Log4Debug(string msg)
        {
            Console.WriteLine("notice:" + msg);
        }
    }
}
