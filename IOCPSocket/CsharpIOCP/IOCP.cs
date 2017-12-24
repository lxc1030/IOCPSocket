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
        const string IP = "127.0.0.1";
        const int portNo = 500;
        const int iClientMaxCount = 1000; ;//最大客户端数量
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
        public void ListenClient()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                s_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s_Server.Bind(new IPEndPoint(ipAddress, portNo));
                s_Server.Listen(iClientMaxCount);
                s_Server.Accept();
                int ibufferSize = 1024; //每个缓冲区大小
                BufferManager bufferManager = new BufferManager(ibufferSize * iClientMaxCount, ibufferSize);
                saeaPool_Receive = new SAEAPool(iClientMaxCount);
                for (int i = 0; i < iClientMaxCount; i++) //填充SocketAsyncEventArgs池
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
                    saeaPool_Receive.Add(saea_New);
                }

                Thread tCheckClientHeartbeat = new Thread(CheckClientHeartbeat);
                tCheckClientHeartbeat.IsBackground = true;
                tCheckClientHeartbeat.Start();

                StartAccept(null);
            }
            catch { }
        }

        /// <summary>
        /// 接受来自客户机的连接请求操作
        /// </summary>
        private void StartAccept(SocketAsyncEventArgs saea_Accept)
        {
            if (saea_Accept == null)
            {
                saea_Accept = new SocketAsyncEventArgs();
                saea_Accept.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            }
            else
                saea_Accept.AcceptSocket = null;  //重用前进行对象清理

            if (!s_Server.AcceptAsync(saea_Accept))
                ProcessAccept(saea_Accept);
        }

        /// <summary>
        /// 连接完成异步操作回调
        /// </summary>
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 接收或发送完成异步操作回调
        /// </summary>
        private void OnIOCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
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
                        Console.WriteLine("Online Client total：" + saeaPool_Receive.GetUsedSAEACount());
                        SAEAUserToken userToken = (SAEAUserToken)saea_Receive.UserToken;
                        userToken.S = s;

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
                        //Console.WriteLine("From the " + sClientIP + " to receive " + e.BytesTransferred + " bytes of data：" + BitConverter.ToString(abFactReceive));
                    }
                    catch { }
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

        /// <summary>
        /// 异步发送操作完成后调用该方法
        /// </summary>
        private void ProcessSend(SocketAsyncEventArgs e)
        {

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
