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

        /// <summary>
        /// 对象池
        /// </summary>
        AsyncUserTokenPool userTokenPool;

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
                s_Server.Bind(new IPEndPoint(ipAddress, portNo));
                s_Server.Listen(maxClient);

                userTokenPool = new AsyncUserTokenPool(maxClient);
                for (int i = 0; i < maxClient; i++) //填充SocketAsyncEventArgs池
                {
                    AsyncUserToken userToken = new AsyncUserToken(bufferSize);

                    userToken.SAEA_Receive.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                    userToken.SAEA_Receive.UserToken = userToken;
                    userToken.SAEA_Send.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                    userToken.SAEA_Send.UserToken = userToken;

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
                    userToken = (AsyncUserToken)e.UserToken;
                    ProcessSend(userToken);
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
                        userToken.HeartbeatTime = DateTime.Now;
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
        private void ProcessReceive(AsyncUserToken userToken)
        {
            SocketAsyncEventArgs e = userToken.SAEA_Receive;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    userToken.HeartbeatTime = DateTime.Now;
                    string sClientIP = ((IPEndPoint)userToken.ConnectSocket.RemoteEndPoint).Address.ToString();
                    try
                    {
                        byte[] abFactReceive = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, e.Offset, abFactReceive, 0, e.BytesTransferred);

                        string info = "";
                        for (int i = 0; i < abFactReceive.Length; i++)
                        {
                            info += abFactReceive[i] + ",";
                        }
                        Log4Debug("From the " + sClientIP + " to receive " + e.BytesTransferred + " bytes of data：" + info);

                        lock (userToken.ReceiveBuffer)
                        {
                            for (int i = 0; i < abFactReceive.Length; i++)
                            {
                                userToken.ReceiveBuffer.Enqueue(abFactReceive[i]);
                            }
                        }

                        byte[] data = abFactReceive;
                        lock (userToken.SendBuffer)
                        {
                            Log4Debug("保存数据:" + data[0]);
                            for (int i = 0; i < data.Length; i++)
                            {
                                userToken.SendBuffer.Enqueue(data[i]);
                            }
                        }
                        Send(userToken);
                    }
                    catch (Exception error)
                    {
                        Log4Debug(error.Message);
                    }
                    finally
                    {
                        if (!userToken.ConnectSocket.ReceiveAsync(e))
                            ProcessReceive(userToken);
                    }
                }
                else
                {
                    CloseClientSocket(userToken);
                }
            }
            catch { }
        }

        private void DealReceive(MessageXieYi xieyi, AsyncUserToken userToken)
        {
            byte[] backInfo = ServerDataManager.instance.SelectMessage(xieyi, userToken); //判断逻辑
            if (backInfo != null)//用户需要服务器返回值的话
            {
                //存储要发送的消息并判断是否发送
                AsyncIOCPServer.instance.SaveSendMessage(userToken, backInfo);
            }
        }









        public void SaveSendMessage(AsyncUserToken userToken, byte[] data)
        {
            //string INFO = "保存待发送:";
            //for (int i = 0; i < data.Length; i++)
            //{
            //    INFO += "_" + data[i];
            //}
            //Log4Debug(INFO);

            lock (userToken.SendBuffer)
            {
                //存值
                for (int i = 0; i < data.Length; i++)
                {
                    //将buffer保存到队列
                    userToken.SendBuffer.Enqueue(data[i]);
                }
            }
            if (!userToken.isSending)
            {
                userToken.isSending = true;
                Send(userToken);
            }

        }



        public void Send(AsyncUserToken userToken)
        {
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
            Socket s = userToken.ConnectSocket;
            SocketAsyncEventArgs e = userToken.SAEA_Send;

            if (!s.SendAsync(e))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
            {
                // 同步发送时处理发送完成事件  
                ProcessSend(userToken);
            }
        }

        /// <summary>
        /// 异步发送操作完成后调用该方法
        /// </summary>
        private void ProcessSend(AsyncUserToken userToken)
        {
            SocketAsyncEventArgs e = userToken.SAEA_Send;
            //TODO  
            if (e.SocketError == SocketError.Success)
            {
                userToken.isSending = false;
                if (userToken.SendBuffer.Count > 0)
                {
                    Send(userToken);
                }
                Log4Debug("发送成功。");
                //TODO
            }
            else
            {
                Log4Debug("发送回调：" + e.SocketError);
            }
        }

        public void SendMessageToUser(string userID, byte[] message, byte xieyiFirst, byte xieyiSecond)
        {
            AsyncUserToken userToken = GetTokenByMemberID(userID);
            if (userToken != null)
            {
                //  创建一个发送缓冲区。   
                MessageXieYi msgXY = new MessageXieYi(xieyiFirst, xieyiSecond, message);
                //Log4Debug("给 ID:" + userID + "/发送消息协议号：" + (MessageConvention)xieyiFirst + "/大小：" + message.Length);
                SaveSendMessage(userToken, msgXY.ToBytes());
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

                ServerDataManager.instance.SetOffLineByState(userToken);
                Log4Debug(String.Format("客户 {0} 清理链接!", userToken.ConnectSocket.RemoteEndPoint.ToString()));
                //
                userToken.ConnectSocket.Shutdown(SocketShutdown.Both);
                Log4Debug("Free Client total：" + userTokenPool.Count());
            }
            catch
            {

            }
            userToken.ConnectSocket = null; //释放引用，并清理缓存，包括释放协议对象等资源
            userToken.userInfo = null;
            userTokenPool.Push(userToken);
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
            LogManager.WriteLog(this.GetType().Name + ":" + msg);
        }

    }
}
