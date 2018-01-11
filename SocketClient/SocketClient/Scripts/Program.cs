﻿using System;
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
            IOCPClient client = new IOCPClient(IP, portNo);

            while (true)
            {
                Console.Write("send>");
                string msg = Console.ReadLine();

                if (!string.IsNullOrEmpty(msg))
                {
                    if (msg == "test")
                    {
                        client.TestThread();
                        for (int t = 0; t < 100; t++)
                        {
                            for (int i = 1; i < 255; i++)
                            {
                                byte[] b = new byte[1024];
                                b[0] = (byte)i;
                                client.SendSave(b);
                            }
                        }
                    }
                    else if (msg == "debug")
                    {
                        client.DebugReceive();
                    }
                    else
                    {
                        client.SendSave(Encoding.Default.GetBytes(msg));
                    }
                }
            }
        }
        

    }
}
