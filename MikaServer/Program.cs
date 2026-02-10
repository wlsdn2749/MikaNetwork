using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ServerCore;

namespace MikaServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Listener listener = new Listener();
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 8080);
            
            listener.Init(endpoint);
            Console.WriteLine("Listening...");
            
            await listener.StartAcceptAsync();
        }
    }
}