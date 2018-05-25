using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketClient
{
    class Program
    {
        public const string IP = "192.168.1.110";
        //public const string IP = "192.168.0.110";
        public const int portNo = 500;

        //static void Main(string[] args)
        //{
        //    IPAddress remote = IPAddress.Parse(IP);
        //    client c = new client(remote, portNo);

        //    c.connect();
        //    Console.WriteLine("服务器连接成功!");
        //    while (true)
        //    {
        //        Console.Write("send>");
        //        string msg = Console.ReadLine();
        //        if (msg == "exit")
        //            break;
        //        if (msg == "xunhuan")
        //        {
        //            for (int i = 0; i < 10000; i++)
        //            {
        //                c.send(msg + ",");
        //            }
        //        }
        //        c.send(msg);
        //    }
        //    c.disconnect();
        //    Console.ReadLine();
        //}
        static void Main(string[] args)
        {
            //IOCPClient client = new IOCPClient(IP, portNo);
            IOCPClient client = null;
            while (true)
            {
                Console.Write("send>");
                string msg = Console.ReadLine();

                if (!string.IsNullOrEmpty(msg))
                {
                    if (msg == "debug")
                    {
                        client.DebugReceive();
                    }
                    else if (msg == "c")
                    {
                        TestClient();
                    }
                    else if (msg == "s")
                    {
                        TestSend();
                    }
                    else if (msg == "n")
                    {
                        client = new IOCPClient(IP, portNo);
                    }
                    else
                    {
                        client.SendSave(Encoding.Default.GetBytes(msg));
                    }
                }
            }
        }
        static List<IOCPClient> all = new List<IOCPClient>();
        static void TestClient()
        {
            for (int i = 0; i < 299; i++)
            {
                IOCPClient client = new IOCPClient(IP, portNo);
                all.Add(client);
            }
            Console.WriteLine("所有连接完成。");
        }

        static void TestSend()
        {
            foreach (IOCPClient item in all)
            {
                item.TestThread();
            }
            Console.WriteLine("所有发送完成。");
        }


    }
}
