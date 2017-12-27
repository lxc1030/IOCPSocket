using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace CsharpIOCP
{
    /// <summary>
    /// SAEA用户标记类
    /// </summary>
   internal class SAEAUserToken
    {
        /// <summary>
        /// 用于发送数据的SocketAsyncEventArgs
        /// </summary>
        public SocketAsyncEventArgs SAEA_Send;
        

        /// <summary>
        /// 连接套接字
        /// </summary>
        public Socket S;
        


    }
}
