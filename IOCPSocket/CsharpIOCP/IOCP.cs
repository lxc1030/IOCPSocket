using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CsharpIOCP
{
    class IOCP
    {
        /// <summary>
        /// Socket-Server
        /// </summary>
        Socket s_Server;

        /// <summary>
        /// 通讯SAEA池
        /// </summary>
        SAEAPool saeaPool_Receive;

        /// <summary>
        /// 侦听客户端
        /// </summary>
        public void ListenClient(string IP, int portNo, int maxClient)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                s_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s_Server.Bind(new IPEndPoint(ipAddress, portNo));
                s_Server.Listen(maxClient);

                int ibufferSize = 1024; //每个缓冲区大小
                BufferManager bufferManager = new BufferManager(ibufferSize * maxClient, ibufferSize);
                saeaPool_Receive = new SAEAPool(maxClient);
                for (int i = 0; i < maxClient; i++) //填充SocketAsyncEventArgs池
                {
                    SocketAsyncEventArgs saea_New = new SocketAsyncEventArgs();
                    saea_New.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                    bufferManager.SetBuffer(saea_New);
                    SAEAUserToken userToken = new SAEAUserToken();
                    SocketAsyncEventArgs saea_Send = new SocketAsyncEventArgs();
                    saea_Send.Completed += new EventHandler<SocketAsyncEventArgs>(OnIOCompleted);
                    userToken.SAEA_Send = saea_Send;
                    userToken.HeartbeatTime = DateTime.Now;
                    saea_New.UserToken = userToken;
                    userToken.SAEA_Send.UserToken = userToken;
                    saeaPool_Receive.Add(saea_New);
                }

                Thread tCheckClientHeartbeat = new Thread(CheckClientHeartbeat);
                tCheckClientHeartbeat.IsBackground = true;
                tCheckClientHeartbeat.Start();

                StartAccept(null);
                Console.WriteLine("初始化服务器。");
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
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
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
            }
        }

        /// <summary>
        /// 异步连接操作完成后调用该方法
        /// </summary>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket s = e.AcceptSocket;
            //if (s != null && s.Connected)
            if (s != null)
            {
                try
                {
                    string sClientIP = ((IPEndPoint)s.RemoteEndPoint).Address.ToString();
                    Console.WriteLine(sClientIP + " Client online");
                    SocketAsyncEventArgs saea_Receive = saeaPool_Receive.Pull();
                    if (saea_Receive != null)
                    {
                        SAEAUserToken userToken = (SAEAUserToken)saea_Receive.UserToken;
                        userToken.S = s;
                        Console.WriteLine("Online Client total：" + saeaPool_Receive.GetUsedSAEACount() + ":" + userToken.HeartbeatTime + "/" + DateTime.Now);

                        if (!userToken.S.ReceiveAsync(saea_Receive))
                            ProcessReceive(saea_Receive);
                    }
                    else
                    {
                        s.Close();
                        Console.WriteLine(sClientIP + " Can't connect server,because connection pool has been finished ！");
                    }
                }
                catch { }
            }
            StartAccept(e);
        }

        /// <summary>
        /// 异步接收操作完成后调用该方法
        /// </summary>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    SAEAUserToken userToken = (SAEAUserToken)e.UserToken;
                    userToken.HeartbeatTime = DateTime.Now;
                    string sClientIP = ((IPEndPoint)userToken.S.RemoteEndPoint).Address.ToString();
                    try
                    {
                        byte[] abFactReceive = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, e.Offset, abFactReceive, 0, e.BytesTransferred);
                        Console.WriteLine("From the " + sClientIP + " to receive " + e.BytesTransferred + " bytes of data：" + BitConverter.ToString(abFactReceive));

                        Send(userToken.SAEA_Send, abFactReceive);
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
                    CloseClientSocket(e);
            }
            catch { }
        }

        public void Send(SocketAsyncEventArgs e, byte[] data)
        {
            SAEAUserToken userToken = (SAEAUserToken)e.UserToken;
            userToken.SAEA_Send.SetBuffer(data, 0, data.Length);
            //autoSendReceiveEvents[SendOperation].WaitOne();
            if (!userToken.S.SendAsync(e))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件  
            {
                // 同步发送时处理发送完成事件  
                ProcessSend(e);
            }
        }

        /// <summary>
        /// 异步发送操作完成后调用该方法
        /// </summary>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            //TODO  
            if (e.SocketError == SocketError.Success)
            {
                SAEAUserToken userToken = (SAEAUserToken)e.UserToken;
                Console.WriteLine("发送成功:" + userToken.HeartbeatTime.ToString());
                //TODO
            }
            else
            {
                Console.WriteLine("发送回调：" + e.SocketError);
            }
        }

        /// <summary>
        /// Socket 断开处理
        /// </summary>
        private void CloseClientSocket(SocketAsyncEventArgs saea)
        {
            try
            {
                SAEAUserToken userToken = (SAEAUserToken)saea.UserToken;
                if (!saeaPool_Receive.Push(saea))
                    return;
                if (userToken.S != null)
                {
                    if (userToken.S.Connected)
                    {
                        try
                        {
                            userToken.S.Shutdown(SocketShutdown.Both);
                        }
                        catch { }
                        string sClientIP = ((IPEndPoint)userToken.S.RemoteEndPoint).Address.ToString();
                        Console.WriteLine(sClientIP + " disconnect ！");
                    }
                    userToken.S.Close();
                }
                Console.WriteLine("Online Client total：" + saeaPool_Receive.GetUsedSAEACount());
            }
            catch { }
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
                    int iCheckInterval = 1000 * 1000; //10秒检测间隔
                    Thread.Sleep(iCheckInterval);
                    List<SocketAsyncEventArgs> lUserdSAEA = saeaPool_Receive.GetUsedSAEA();
                    if (lUserdSAEA != null && lUserdSAEA.Count > 0)
                    {
                        foreach (SocketAsyncEventArgs saea in lUserdSAEA)
                        {
                            SAEAUserToken userToken = (SAEAUserToken)saea.UserToken;
                            if (userToken.HeartbeatTime.AddMilliseconds(iCheckInterval).CompareTo(DateTime.Now) < 0)
                            {
                                if (userToken.S != null)
                                {
                                    try
                                    {
                                        string sClientIP = ((IPEndPoint)userToken.S.RemoteEndPoint).Address.ToString();
                                        Console.WriteLine(sClientIP + " the heartbeat timeout ！");
                                    }
                                    catch { }
                                    userToken.S.Close(); //服务端主动关闭心跳超时连接，在此关闭连接，会触发OnIOCompleted回调                                    
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
