using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// 用户对象
/// </summary>
public class AsyncUserToken
{
    #region 字段

    public SocketAsyncEventArgs AsyncReceive { get; set; }
    public SocketAsyncEventArgs AsyncSend { get; set; }

    private Queue<byte[]> _receiveBuffer;
    public Queue<byte[]> ReceiveBuffer
    {
        get { return _receiveBuffer; }
        set { _receiveBuffer = value; }
    }

    private Queue<byte[]> _sendBuffer;
    public Queue<byte[]> SendBuffer
    {
        get { return _sendBuffer; }
        set { _sendBuffer = value; }
    }

    private byte[] _halfMessage;
    public byte[] HalfMessage
    {
        get { return _halfMessage; }
        set { _halfMessage = value; }
    }


    ///// <summary>
    ///// 用户数据
    ///// </summary>
    //public RoomActor userInfo;


    #endregion

    #region 属性

    /// <summary>
    /// 连接的Socket对象
    /// </summary>
    private Socket _connectSocket;
    public Socket ConnectSocket
    {
        get { return _connectSocket; }
        set { _connectSocket = value; }
    }

    #endregion

    public AsyncUserToken(EventHandler eventHandle)
    {
        _connectSocket = null;

        _receiveBuffer = new Queue<byte[]>();
        _sendBuffer = new Queue<byte[]>();
        _halfMessage = new byte[] { };

        AsyncReceive = new SocketAsyncEventArgs();
        AsyncSend = new SocketAsyncEventArgs();
        AsyncReceive.Completed += new EventHandler<SocketAsyncEventArgs>(eventHandle);
        AsyncSend.Completed += new EventHandler<SocketAsyncEventArgs>(eventHandle);
    }

}
