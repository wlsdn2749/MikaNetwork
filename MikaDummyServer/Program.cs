using System.Net;
using MikaServerCore.Network;

namespace MikaDummyServer
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var server = new MikaServer(10010);
            var packetManager = new PacketManager();

            server.PacketReceived += (session, data) =>
            {
                packetManager.OnRecvPacket(session, data);

                return ValueTask.CompletedTask;
            };
            
            server.Listen();

            Console.WriteLine($"[Server] 10010 포트에서 대기 중... (종료하려면 엔터를 누르세요)");
            
            Console.WriteLine("[Server] 종료하려면 엔터를 누르세요.");
            Console.ReadLine(); // 대기
        }
    }
}
