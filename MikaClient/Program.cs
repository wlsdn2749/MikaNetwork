using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MikaServerCore;

namespace MikaClient
{
    class Program
    {
        static async Task Main(String[] args)
        {
            MikaConnector connector = new MikaConnector();
            connector.Connect("127.0.0.1", 7777);
            
            connector.Send("Hello");
            
            while (true)
            {
                connector.Receive();
            }
        }
    }
}