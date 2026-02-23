using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MikaServerCore.Network;

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

            listener.OnAcceptHandler = (socket) =>
            {
                var session = new Session();
                session.Init(socket);

                session.OnPackageReceived = (s, p) =>
                {
                    _ = s.SendLineAsync($"echo: {p.Text}");
                };
                
                _ = session.StartAsync();
            };
            
            await listener.StartAcceptAsync();
        }
    }
}