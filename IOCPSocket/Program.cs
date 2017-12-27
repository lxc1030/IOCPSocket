using System;
using System.Net;

namespace CsharpIOCP
{
    class Program
    {
        //public const string IP = "192.168.1.110";
        public const string IP = "192.168.0.110";
        public const int portNo = 500;

        static void Main(string[] args)
        {
            IOCPServer iocp = new IOCPServer(IPAddress.Parse(IP), portNo, 20);
            iocp.Start();
            Console.WriteLine("服务器已启动....");
            System.Console.ReadLine();
        }
    }
}
