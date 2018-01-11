using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CsharpIOCP
{
    class AsyncIOCPServer
    {
        public static AsyncIOCPServer instance;
        /// <summary>
        /// Socket-Server
        /// </summary>
        Socket s_Server;
        // Listener endpoint.  
        private IPEndPoint hostEndPoint;

        /// <summary>
        /// 对象池
        /// </summary>
        AsyncUserTokenPool userTokenPool;

        //发送与接收的MySocketEventArgs变量定义.  
        private List<MySocketEventArgs> listArgs = new List<MySocketEventArgs>();

        /// <summary>
        /// 每个Socket套接字缓冲区大小
        /// </summary>
        int bufferSize = 1024;
        /// <summary>
        /// 心跳检测间隔秒数
        /// </summary>
        int HeartbeatSecondTime = 60;


        /// <summary>
        /// 侦听客户端
        /// </summary>
        public AsyncIOCPServer(string IP, int portNo, int maxClient)
        {
            instance = this;
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                s_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                hostEndPoint = new IPEndPoint(ipAddress, portNo);
                s_Server.Bind(hostEndPoint);
                s_Server.Listen(maxClient);

                userTokenPool = new AsyncUserTokenPool(maxClient);
                for (int i = 0; i < maxClient; i++) //填充SocketAsyncEventArgs池
                {
                    AsyncUserToken userToken = new AsyncUserToken(bufferSize);

                    userToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                    userToken.SAEA_Receive.UserToken = userToken;
                    userTokenPool.Push(userToken);
                }

                Thread tCheckClientHeartbeat = new Thread(CheckClientHeartbeat);
                tCheckClientHeartbeat.IsBackground = true;
                tCheckClientHeartbeat.Start();

                StartAccept(null);
                Log4Debug("初始化服务器。");
            }
            catch (Exception error)
            {
                Log4Debug(error.Message);
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
            sendArg.UserToken = s_Server;
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




        /// <summary>
        /// 接受来自客户机的连接请求操作
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs saea_Accept)
        {
            if (saea_Accept == null)
            {
                saea_Accept = new SocketAsyncEventArgs();
                saea_Accept.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
            }
            else
                saea_Accept.AcceptSocket = null;  //重用前进行对象清理

            if (!s_Server.AcceptAsync(saea_Accept))
                ProcessAccept(saea_Accept);
        }

        /// <summary>
        /// 接收或发送完成异步操作回调
        /// </summary>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = null;
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
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

        /// <summary>
        /// 异步连接操作完成后调用该方法
        /// </summary>
        private void ProcessAccept(SocketAsyncEventArgs accept)
        {
            Socket s = accept.AcceptSocket;
            if (s != null)
            {
                try
                {
                    string sClientIP = ((IPEndPoint)s.RemoteEndPoint).Address.ToString();
                    Log4Debug(sClientIP + " Client Accept");

                    AsyncUserToken userToken = userTokenPool.Pop();
                    if (userToken != null)
                    {
                        userToken.ConnectSocket = s;
                        //userToken.HeartbeatTime = DateTime.Now;
                        Log4Debug("Free Client total：" + userTokenPool.Count());
                        SocketAsyncEventArgs e = userToken.SAEA_Receive;
                        if (!userToken.ConnectSocket.ReceiveAsync(e))
                        {
                            ProcessReceive(userToken);
                        }
                    }
                    else
                    {
                        s.Close();
                        Log4Debug(sClientIP + " Can't connect server,because connection pool has been finished ！");
                    }
                }
                catch { }
            }
            StartAccept(accept);
        }

        /// <summary>
        /// 异步接收操作完成后调用该方法
        /// </summary>
        //private void ProcessReceive(SocketAsyncEventArgs e)
        //{
        //    try
        //    {
        //        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        //        {
        //            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
        //            userToken.HeartbeatTime = DateTime.Now;
        //            string sClientIP = ((IPEndPoint)userToken.S.RemoteEndPoint).Address.ToString();
        //            try
        //            {
        //                byte[] abFactReceive = new byte[e.BytesTransferred];
        //                Array.Copy(e.Buffer, e.Offset, abFactReceive, 0, e.BytesTransferred);

        //                string info = "";
        //                for (int i = 0; i < abFactReceive.Length; i++)
        //                {
        //                    info += abFactReceive[i] + ",";
        //                }
        //                Log4Debug("From the " + sClientIP + " to receive " + e.BytesTransferred + " bytes of data：" + info);

        //                lock (userToken.ReceiveBuffer)
        //                {
        //                    for (int i = 0; i < abFactReceive.Length; i++)
        //                    {
        //                        userToken.ReceiveBuffer.Enqueue(abFactReceive[i]);
        //                    }
        //                }

        //                byte[] data = abFactReceive;
        //                lock (userToken.SendBuffer)
        //                {
        //                    Log4Debug("保存数据:" + data[0]);
        //                    for (int i = 0; i < data.Length; i++)
        //                    {
        //                        userToken.SendBuffer.Enqueue(data[i]);
        //                    }
        //                }
        //                Send(userToken.SAEA_Send);
        //            }
        //            catch (Exception error)
        //            {
        //                Log4Debug(error.Message);
        //            }
        //            finally
        //            {
        //                if (!userToken.S.ReceiveAsync(e))
        //                    ProcessReceive(e);
        //            }
        //        }
        //        else
        //        {
        //            CloseClientSocket(e);
        //        }
        //    }
        //    catch { }
        //}
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
                    string debug = "Receve " + copy.Length + " :";
                    for (int i = 0; i < copy.Length; i++)
                    {
                        debug += copy[i] + ",";
                    }
                    Log4Debug(debug);
                    //
                    //lock (userToken.ReceiveBuffer)
                    //{
                    //    userToken.ReceiveBuffer.AddRange(copy);
                    //}
                    if (!userToken.ConnectSocket.ReceiveAsync(e))
                        ProcessReceive(userToken);

                    //if (!userToken.isDealReceive)
                    //{
                    //    userToken.isDealReceive = true;
                    //    Handle(userToken);
                    //}
                }
                //catch (Exception error)
                //{
                //    Log4Debug(error.Message);
                //}
            }
            else
            {
                CloseClientSocket(userToken);
            }
        }
        private void Handle(AsyncUserToken userToken)
        {
            while (userToken.ReceiveBuffer.Count > 0)
            {
                byte[] rece = null;
                lock (userToken.ReceiveBuffer)
                {
                    rece = userToken.ReceiveBuffer.ToArray();
                    userToken.ReceiveBuffer.Clear();
                }
                userToken.DealBuffer.AddRange(rece);
                //
                while (userToken.DealBuffer.Count > 0)
                {
                    if (userToken.DealBuffer.Count < sizeof(int))
                    {
                        break;
                    }
                    byte[] buffer = null;

                    byte[] lengthB = new byte[sizeof(int)];
                    lengthB = userToken.DealBuffer.Take(sizeof(int)).ToArray();
                    int length = BitConverter.ToInt32(lengthB, 0);
                    if (userToken.DealBuffer.Count < length + sizeof(int))
                    {
                        //Log4Debug("还未收齐，继续接收");
                        break;
                    }
                    else
                    {
                        userToken.DealBuffer.RemoveRange(0, sizeof(int));
                        buffer = userToken.DealBuffer.Take(length).ToArray();
                        userToken.DealBuffer.RemoveRange(0, length);
                    }

                    string debug = "Receve:";
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        debug += buffer[i] + ",";
                    }
                    Console.WriteLine(debug);

                    DealReceive(null, userToken);
                    //while (buffer.Length > 0)
                    //{
                    //    MessageXieYi xieyi = MessageXieYi.FromBytes(buffer);
                    //    if (xieyi != null)
                    //    {
                    //        int messageLength = xieyi.MessageContentLength + MessageXieYi.XieYiLength + 1 + 1;
                    //        buffer = buffer.Skip(messageLength).ToArray();
                    //        DealReceive(xieyi, userToken);
                    //    }
                    //    else
                    //    {
                    //        string info = "数据应该直接处理完，不会到这:";
                    //        for (int i = 0; i < buffer.Length; i++)
                    //        {
                    //            info += buffer[i] + ",";
                    //        }
                    //        Log4Debug(info);
                    //        break;
                    //    }
                    //}
                }
            }
            userToken.isDealReceive = false;
        }
        #endregion

        private void DealReceive(MessageXieYi xieyi, AsyncUserToken userToken)
        {
            //byte[] backInfo = ServerDataManager.instance.SelectMessage(xieyi, userToken); //判断逻辑
            //if (backInfo != null)//用户需要服务器返回值的话
            //{
            //    //存储要发送的消息并判断是否发送
            //    AsyncIOCPServer.instance.SaveSendMessage(userToken, backInfo);
            //}
            SendSave(userToken, new byte[] { 1 });
        }




        public void SendSave(AsyncUserToken userToken, byte[] data)
        {
            Send(userToken, data);
        }


     

        /// <summary>
        /// 异步发送操作完成后调用该方法
        /// </summary>
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
        public void Send(AsyncUserToken userToken, byte[] buffer)
        {
            //string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).ToString();
            //string info = "";
            //for (int i = 0; i < buffer.Length; i++)
            //{
            //    info += buffer[i] + ",";
            //}
            //Log4Debug("From the " + sClientIP + " to send " + buffer.Length + " bytes of data：" + info);

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

            Log4Debug("发送所用的套接字编号：" + sendArgs.ArgsTag);
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
        /// 获取指定用户
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public AsyncUserToken GetTokenByMemberID(string userID)
        {
            return userTokenPool.GetTokenByMemberID(userID);
        }



        /// <summary>
        /// Socket 断开处理
        /// </summary>
        private void CloseClientSocket(AsyncUserToken userToken)
        {
            try
            {
                if (userToken.ConnectSocket == null)
                    return;

                //ServerDataManager.instance.SetOffLineByState(userToken);
                Log4Debug(String.Format("客户 {0} 清理链接!", userToken.ConnectSocket.RemoteEndPoint.ToString()));
                //
                userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
                userToken.ConnectSocket.Close();
                Log4Debug("Free Client total：" + userTokenPool.Count());
            }
            catch
            {

            }
            finally
            {
                //userTokenPool.RemoveUsed(userToken);//清除在线
                userToken.Init();//清除该变量
                userTokenPool.Push(userToken);//复存该变量
            }
        }

        /// <summary>
        /// 客户端心跳检测
        /// </summary>
        private void CheckClientHeartbeat()
        {
            while (true)
            {
                try
                {
                    int heartbeatTime = HeartbeatSecondTime * 1000; //1000是毫秒，检测间隔
                    Thread.Sleep(heartbeatTime);
                    Log4Debug("开始心跳检测" + DateTime.Now);
                    userTokenPool.CheckIsConnected(heartbeatTime, CloseClientSocket);
                }
                catch (Exception e)
                {
                    Log4Debug("心跳检测错误:" + e.Message);
                }
            }
        }

        public void Log4Debug(string msg)
        {
            LogManager.instance.WriteLog(this.GetType().Name + ":" + msg);
        }

    }
}
