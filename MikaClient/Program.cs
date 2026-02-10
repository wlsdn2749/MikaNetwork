using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ServerCore;


namespace MikaClient
{
    class Program
    {
        static async Task Main(String[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8080;

            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            Session session = new();

            await session.ConnectAsync(ipEndPoint);

            while (true)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;
                if (input == "exit") break;

                byte[] sendBuffer = Encoding.UTF8.GetBytes(input);
                await session.SendAsync(sendBuffer);
            }
            
            session.Disconnect();
        }
    }
}