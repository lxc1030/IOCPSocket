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
        /// 用于每个I/O Socket操作的缓冲区大小 默认1024
        /// </summary>
        private int bufferSize = 1024;
        /// <summary>  
        /// 连接服务器的socket  
        /// </summary>  
        private Socket _clientSock;
        // Signals a connection.
        private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);

        //发送与接收的MySocketEventArgs变量定义.  
        private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();

        /// <summary>  
        /// 用于服务器执行的互斥同步对象  
        /// </summary>  
        private static Mutex mutex = new Mutex();
        /// <summary>  
        /// Socket连接标志  
        /// </summary>  
        private Boolean isConnected = false;

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





        #region 客户端特有的Connect

        /// <summary>
        /// 连接到主机
        /// </summary>
        /// <returns>0.连接成功, 其他值失败,参考SocketError的值列表</returns>
        internal SocketError Connect()
        {
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = hostEndPoint;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            //connectArgs.Completed += (SocketAsyncEventArgs, Action) =>
            //{
            //    OnConnect(callback, connectArgs);
            //};
            if (!_clientSock.ConnectAsync(connectArgs))
            {
                ProcessConnected(connectArgs);
            }
            autoConnectEvent.WaitOne();

            return connectArgs.SocketError;
        }




        // Calback for connect operation
        private void ProcessConnected(SocketAsyncEventArgs e)
        {
            // Signals the end of connection.
            autoConnectEvent.Set(); //释放阻塞.
                                    // Set the flag for socket connected.
            isConnected = (e.SocketError == SocketError.Success);
            if (isConnected)
            {
                //Debug.Log("Socket连接成功");

                MyUserToken = new AsyncUserToken(bufferSize);
                MyUserToken.ConnectSocket = _clientSock;

                MyUserToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                MyUserToken.SAEA_Receive.UserToken = MyUserToken;

                if (!MyUserToken.ConnectSocket.ReceiveAsync(MyUserToken.SAEA_Receive))
                {
                    ProcessReceive(MyUserToken);
                }
            }
            else
            {
                Log4Debug("Socket连接失败:" + e.SocketError);
            }
        }


        int tagCount = 0;
        /// <summary>  
        /// 初始化发送参数MySocketEventArgs  
        /// </summary>  
        /// <returns></returns>  
        MySocketEventArgs initSendArgs()
        {
            MySocketEventArgs sendArg = new MySocketEventArgs();
            sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            sendArg.UserToken = _clientSock;
            sendArg.RemoteEndPoint = hostEndPoint;
            sendArg.IsUsing = false;
            Interlocked.Increment(ref tagCount);
            sendArg.ArgsTag = tagCount;
            lock (listArgs)
            {
                listArgs.Add(sendArg);
            }
            return sendArg;
        }


        #endregion






        //#region 连接服务器  

        ///// <summary>  
        ///// 连接远程服务器  
        ///// </summary>  
        //public void Connect()
        //{
        //    MyUserToken = new AsyncUserToken();
        //    MyUserToken.S = _clientSock;

        //    SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
        //    connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);

        //    connectArgs.UserToken = MyUserToken;
        //    connectArgs.RemoteEndPoint = hostEndPoint;


        //    SocketAsyncEventArgs saea_Send = new SocketAsyncEventArgs();
        //    saea_Send.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);

        //    MyUserToken.SAEA_Send = saea_Send;

        //    saea_Send.UserToken = MyUserToken;


        //    //mutex.WaitOne();
        //    if (!_clientSock.ConnectAsync(connectArgs))//异步连接  
        //    {
        //        ProcessConnected(connectArgs);
        //    }
        //}


        ///// <summary>  
        ///// 处理连接服务器  
        ///// </summary>  
        ///// <param name="e"></param>  
        //private void ProcessConnected(SocketAsyncEventArgs e)
        //{
        //    //TODO  
        //    if (e.SocketError == SocketError.Success)
        //    {
        //        Log4Debug("连接成功。");
        //        AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
        //        e.AcceptSocket = userToken.S;
        //        //Socket s = e.AcceptSocket;//和客户端关联的socket
        //        Socket s = userToken.S;
        //        if (s.Connected)
        //        {
        //            try
        //            {
        //                byte[] receiveBuffer = new byte[1024];
        //                e.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
        //                //autoSendReceiveEvents[ReceiveOperation].WaitOne();
        //                if (!s.ReceiveAsync(e))
        //                {
        //                    ProcessReceive(e);
        //                }
        //            }
        //            catch (SocketException ex)
        //            {
        //                //TODO 异常处理
        //            }
        //        }
        //    }
        //}

        //#endregion

        /// <summary>
        /// 接收或发送完成异步操作回调
        /// </summary>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = null;
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnected(e);
                    break;
                case SocketAsyncOperation.Accept:
                    //ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    userToken = (AsyncUserToken)e.UserToken;
                    ProcessReceive(userToken);
                    break;
                case SocketAsyncOperation.Send:
                    //userToken = (AsyncUserToken)e.UserToken;
                    ProcessSend((MySocketEventArgs)e);
                    break;
            }
        }



        #region 发送消息  
        public void SendSave(byte[] data)
        {
            SendSave(MyUserToken, data);
        }
        /// <summary>  
        /// 向服务器发送消息  
        /// </summary>  
        /// <param name="data"></param>  
        public void SendSave(AsyncUserToken userToken, byte[] data)
        {
            Send(userToken, data);
        }
        public void Send(AsyncUserToken userToken, byte[] buffer)
        {
            //查找有没有空闲的发送MySocketEventArgs,有就直接拿来用,没有就创建新的.So easy!  
            MySocketEventArgs sendArgs = null;

            lock (listArgs)
            {
                sendArgs = listArgs.Find(a => a.IsUsing == false);
                if (sendArgs == null)
                {
                    sendArgs = initSendArgs();
                }
                sendArgs.IsUsing = true;
            }

            //Log4Debug("发送所用的套接字编号：" + sendArgs.ArgsTag);
            //lock (sendArgs) //要锁定,不锁定让别的线程抢走了就不妙了.  
            {
                sendArgs.SetBuffer(buffer, 0, buffer.Length);
            }
            Socket s = userToken.ConnectSocket;
            if (!s.SendAsync(sendArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
            {
                // 同步发送时处理发送完成事件  
                ProcessSend(sendArgs);
            }
        }



        /// <summary>  
        /// 发送完成时处理函数  
        /// </summary>  
        /// <param name="e">与发送完成操作相关联的SocketAsyncEventArg对象</param>  
        private void ProcessSend(MySocketEventArgs e)
        {
            //SocketAsyncEventArgs e = userToken.SAEA_Send;
            if (e.SocketError == SocketError.Success)
            {
                e.IsUsing = false;
            }
            else
            {
                Log4Debug("发送未成功，回调：" + e.SocketError);
            }
        }
        #endregion


        #region Receive
        private void ProcessReceive(AsyncUserToken userToken)
        {
            SocketAsyncEventArgs e = userToken.SAEA_Receive;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //if (userToken.userInfo != null)
                //{
                //    userToken.userInfo.heartbeatTime = DateTime.Now;
                //}
                string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).Address.ToString();
                //try
                {
                    byte[] copy = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, copy, 0, e.BytesTransferred);

                    if (!userToken.ConnectSocket.ReceiveAsync(e))
                        ProcessReceive(userToken);

                    ConnCache connCache = new ConnCache(copy, userToken);
                    //ConnCache connCache = new ConnCache(e.Buffer, userToken);
                    //处理线程
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AnalyzeThrd), connCache);
                }
                //catch (Exception error)
                //{
                //    Log4Debug(error.Message);
                //}
            }
            else
            {
                //CloseClientSocket(userToken);
            }
        }

        /// <summary>
        /// 线程处理接收事件
        /// </summary>
        /// <param name="state"></param>
        private void AnalyzeThrd(object state)
        {
            ConnCache connCache = (ConnCache)state;
            AsyncUserToken userToken = connCache.UserToken;
            lock (userToken.AnalyzeLock)
            {
                lock (userToken.ReceiveBuffer)
                {
                    userToken.ReceiveBuffer.AddRange(connCache.RecvBuffer);
                }
                Handle(userToken);
            }
        }
        private void Handle(AsyncUserToken userToken)
        {
            do
            {
                byte[] lenBytes = userToken.ReceiveBuffer.GetRange(0, sizeof(int)).ToArray();
                int packageLen = BitConverter.ToInt32(lenBytes, 0);
                if (packageLen <= userToken.ReceiveBuffer.Count - sizeof(int))
                {
                    //包够长时,则提取出来,交给后面的程序去处理  
                    byte[] buffer = userToken.ReceiveBuffer.GetRange(sizeof(int), packageLen).ToArray();
                    //从数据池中移除这组数据,为什么要lock,你懂的  
                    lock (userToken.ReceiveBuffer)
                    {
                        userToken.ReceiveBuffer.RemoveRange(0, packageLen + sizeof(int));
                    }

                    while (buffer.Length > 0)
                    {
                        if (buffer[0] != MessageXieYi.markStart)
                        {
                            break;
                        }
                        MessageXieYi xieyi = MessageXieYi.FromBytes(buffer);
                        if (xieyi == null)
                        {
                            Log4Debug("奇怪为什么协议为空");
                            break;
                        }
                        int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                        buffer = buffer.Skip(messageLength).ToArray();
                        //将数据包交给前台去处理
                        //DealXieYi(xieyi, userToken);
                        //Log4Debug(xieyi.XieYiFirstFlag + ".");
                    }
                }
                else
                {   //长度不够,还得继续接收,需要跳出循环  
                    break;
                }
            } while (userToken.ReceiveBuffer.Count > sizeof(int));
        }

        #endregion

        public void TestThread()
        {
            Thread t = new Thread(WriteY);
            t.Start();
        }
        void WriteY()
        {
            for (int i = 0; i < 100; i++)
            {
                byte j = (byte)i;
                MessageXieYi xieyi = new MessageXieYi(1, 0, new byte[5] { j, j, j, j, j });
                byte[] send = xieyi.ToBytes();
                byte[] buffer = AsyncUserToken.GetSendBytes(send);
                SendSave(buffer);
            }
            Console.WriteLine("本次循环完成。");
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
