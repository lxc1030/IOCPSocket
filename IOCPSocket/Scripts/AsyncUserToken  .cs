using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class AsyncUserToken
{
    public Socket connectSocket { get; set; }

    public SocketAsyncEventArgs AsyncSend { get; set; }
    public SocketAsyncEventArgs AsyncReceive { get; set; }

    public byte[] byteReceive { get; set; }
    public byte[] byteSend { get; set; }

    public DateTime HeartbeatTime { get; set; }


    public AsyncUserToken(int bufferSize)
    {
        AsyncSend = new SocketAsyncEventArgs();
        byteSend = new byte[bufferSize];
        AsyncSend.SetBuffer(byteSend, 0, bufferSize);
        AsyncSend.UserToken = null;
        AsyncReceive = new SocketAsyncEventArgs();
        byteReceive = new byte[bufferSize];
        AsyncReceive.SetBuffer(byteReceive, 0, bufferSize);
        AsyncReceive.UserToken = null;
    }

  
}
