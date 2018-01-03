using System;
using System.Net;

namespace CsharpIOCP
{
    class Program
    {
        //public const string IP = "192.168.1.110";
        public const string IP = "192.168.0.110";
        public const int portNo = 500;
        const int iClientMaxCount = 20;//最大客户端数量

        static void Main(string[] args)
        {
            IOCPDataManager datas = new IOCPDataManager();
            IOCPServer iocp = new IOCPServer(IP, portNo, iClientMaxCount);
            Console.WriteLine("服务器已启动....");
            System.Console.ReadLine();
        }
    }
}
