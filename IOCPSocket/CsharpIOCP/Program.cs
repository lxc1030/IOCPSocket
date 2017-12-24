using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsharpIOCP
{
    class Program
    {
        static void Main(string[] args)
        {
            IOCP iocp = new IOCP();
            iocp.ListenClient();
            Console.ReadLine();
        }       
    }
}
