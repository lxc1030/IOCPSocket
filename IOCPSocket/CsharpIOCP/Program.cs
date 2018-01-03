using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsharpIOCP
{
    class Program
    {
        public const string IP = "192.168.0.110";
        public const int portNo = 500;
        const int iClientMaxCount = 20;//最大客户端数量

        static void Main(string[] args)
        {
            IOCP iocp = new IOCP();
            iocp.ListenClient(IP, portNo, iClientMaxCount);
            Console.ReadLine();
        }
    }
}
