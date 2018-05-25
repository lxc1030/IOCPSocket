using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// SAEA用户标记类
/// </summary>
public class AsyncUserToken
{
    public SocketAsyncEventArgs SAEA_Receive;

    private byte[] byteReceive { get; set; }
    private byte[] byteSend { get; set; }


    /// <summary>
    /// 接收数据的缓冲区
    /// </summary>
    private List<byte> _receiveBuffer;
    public List<byte> ReceiveBuffer
    {
        get { return _receiveBuffer; }
        set { _receiveBuffer = value; }
    }
    /// <summary>
    /// 发送数据的缓冲区
    /// </summary>
    private List<byte> _sendBuffer;
    public List<byte> SendBuffer
    {
        get { return _sendBuffer; }
        set { _sendBuffer = value; }
    }


    public readonly object AnalyzeLock = new object();      // 数据分析锁
    public List<byte> DealBuffer { get; set; }


    /// <summary>
    /// 连接套接字
    /// </summary>
    private Socket _connectSocket;
    public Socket ConnectSocket
    {
        get { return _connectSocket; }
        set { _connectSocket = value; }
    }

    public bool isDealReceive { get; set; }

    /// <summary>
    /// 用户数据
    /// </summary>
    //public RoomActor userInfo { get; set; }


    public AsyncUserToken(int size)
    {
        Init();

        byteReceive = new byte[size];
        SAEA_Receive = new SocketAsyncEventArgs();
        SAEA_Receive.SetBuffer(byteReceive, 0, size);

        byteSend = new byte[size];
    }

    public void Init()
    {
        //userInfo = null;

        _receiveBuffer = new List<byte>();
        _sendBuffer = new List<byte>();
        DealBuffer = new List<byte>();

        isDealReceive = false;

        ConnectSocket = null;
    }

    //public static int lengthLength = 4;


    //public byte[] GetSendBytes()
    //{
    //    List<byte> send = null;
    //    lock (SendBuffer)
    //    {
    //        send = new List<byte>();

    //        int length = SendBuffer.Count;
    //        byte[] lengthB = BitConverter.GetBytes(length);
    //        send.AddRange(lengthB);

    //        byte[] body = SendBuffer.ToArray();
    //        send.AddRange(body);
    //        //
    //        SendBuffer.Clear();
    //    }
    //    return send.ToArray();
    //}
    public static byte[] GetSendBytes(byte[] buffer)
    {
        int length = buffer.Length;
        byte[] send = new byte[buffer.Length + sizeof(int)];

        byte[] temp = BitConverter.GetBytes(length);

        Array.Copy(temp, 0, send, 0, sizeof(int));
        Array.Copy(buffer, 0, send, sizeof(int), length);

        return send.ToArray();
    }

}
public class MySocketEventArgs : SocketAsyncEventArgs
{

    /// <summary>  
    /// 标识，只是一个编号而已  
    /// </summary>  
    public int ArgsTag { get; set; }

    /// <summary>  
    /// 设置/获取使用状态  
    /// </summary>  
    public bool IsUsing { get; set; }

}
/// <summary>
/// 每次接收到的数据，放进队列中等待线程池处理
/// </summary>
public sealed class ConnCache
{
    //public uint SocketId;                     // 连接标识
    public AsyncUserToken UserToken;

    public byte[] RecvBuffer;                   // 接收数据缓存，传输层抵达的数据，首先进入此缓存       
    //public int RecvLen;                       // 接收数据缓存中的有效数据长度
    //public readonly object RecvLock;          // 数据接收锁

    //public byte[] WaitBuffer;                 // 待处理数据缓存，首先要将数据从RecvBuffer转移到该缓存，数据处理线程才能进行处理
    //public int WaitLen;                       // 待处理数据缓存中的有效数据长度
    //public readonly object AnalyzeLock;       // 数据分析锁

    public ConnCache(byte[] receive, AsyncUserToken _userToken)
    {

        //SocketId = 65535;
        //RecvBuffer = new byte[recvBuffSize];
        RecvBuffer = receive;
        UserToken = _userToken;
        //RecvLen = 0;
        //RecvLock = new object();
        //WaitBuffer = new byte[waitBuffSize];
        //WaitLen = 0;
        //AnalyzeLock = new object();
    }
}