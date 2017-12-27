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
        private IPEndPoint _remoteEndPoint;

        public IOCPClient(IPEndPoint local, IPEndPoint remote)
        {
            _clientSock = new Socket(local.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _remoteEndPoint = remote;
        }

        #region 连接服务器  

        /// <summary>  
        /// 连接远程服务器  
        /// </summary>  
        public void Connect()
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
     
            connectArgs.UserToken = _clientSock;
            connectArgs.RemoteEndPoint = _remoteEndPoint;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnected);
            //mutex.WaitOne();
            if (!_clientSock.ConnectAsync(connectArgs))//异步连接  
            {
                ProcessConnected(connectArgs);
            }

        }
        /// <summary>  
        /// 连接上的事件  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            //mutex.ReleaseMutex();
            //设置Socket已连接标志。   
            _connected = (e.SocketError == SocketError.Success);
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
                Socket s = e.AcceptSocket;//和客户端关联的socket
                if (s.Connected)
                {
                    try
                    {
                        SocketAsyncEventArgs asyniar = new SocketAsyncEventArgs();
                        asyniar.UserToken = s;
                        asyniar.AcceptSocket = s;
                        
                        if (!s.ReceiveAsync(asyniar))//投递接收请求
                        {
                            ProcessReceive(asyniar);
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
        public void Send(byte[] data)
        {
            SocketAsyncEventArgs asyniar = new SocketAsyncEventArgs();
            asyniar.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendComplete);
            asyniar.SetBuffer(data, 0, data.Length);
            asyniar.UserToken = _clientSock;
            asyniar.RemoteEndPoint = _remoteEndPoint;
            //autoSendReceiveEvents[SendOperation].WaitOne();
            if (!_clientSock.SendAsync(asyniar))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
            {
                // 同步发送时处理发送完成事件  
                ProcessSend(asyniar);
            }
        }

        /// <summary>  
        /// 发送操作的回调方法  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void OnSendComplete(object sender, SocketAsyncEventArgs e)
        {
            //发出发送完成信号。   
            autoSendReceiveEvents[SendOperation].Set();
            ProcessSend(e);
        }

        /// <summary>  
        /// 发送完成时处理函数  
        /// </summary>  
        /// <param name="e">与发送完成操作相关联的SocketAsyncEventArg对象</param>  
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            //TODO  
        }
        #endregion

        #region 接收消息  
        /// <summary>  
        /// 开始监听服务端数据  
        /// </summary>  
        /// <param name="e"></param>  
        public void StartRecive(SocketAsyncEventArgs e)
        {
            //准备接收。   
            Socket s = e.UserToken as Socket;
            byte[] receiveBuffer = new byte[255];
            e.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            e.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveComplete);
            autoSendReceiveEvents[ReceiveOperation].WaitOne();
            if (!s.ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }

        /// <summary>  
        /// 接收操作的回调方法  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void OnReceiveComplete(object sender, SocketAsyncEventArgs e)
        {
            //发出接收完成信号。   
            autoSendReceiveEvents[ReceiveOperation].Set();
            ProcessReceive(e);
        }

        /// <summary>  
        ///接收完成时处理函数  
        /// </summary>  
        /// <param name="e">与接收完成操作相关联的SocketAsyncEventArg对象</param>  
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 检查远程主机是否关闭连接  
                if (e.BytesTransferred > 0)
                {
                    Socket s = (Socket)e.UserToken;
                    //判断所有需接收的数据是否已经完成  
                    if (s.Available == 0)
                    {
                        byte[] data = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, e.Offset, data, 0, data.Length);//从e.Buffer块中复制数据出来，保证它可重用  

                        //TODO 处理数据  
                        string info = Encoding.Default.GetString(data);
                        Log4Debug(String.Format("收到 {0} 数据为 {1}", s.RemoteEndPoint.ToString(), info));
                    }

                    if (!s.ReceiveAsync(e))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
                    {
                        //同步接收时处理接收完成事件  
                        ProcessReceive(e);
                    }
                }
            }
        }

        #endregion


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
