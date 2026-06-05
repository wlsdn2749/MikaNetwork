using System;
using System.Threading.Tasks;
using MikaProtocol;
using MikaServerCore.Network;

namespace MikaDummyClient
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var client = new MikaClient();

            var packetManager = new PacketManager();
            packetManager.Register<S_EchoResponse>(PacketId.S_EchoResponse, PacketHandler.Handle_S_EchoResponse);

            client.PacketReceived += (session, data) =>
            {
                packetManager.OnRecvPacket(session, data);
                return ValueTask.CompletedTask;
            };

            await client.ConnectAsync("127.0.0.1", 10010);

            Console.WriteLine("[Client] 보낼 메시지를 입력하고 [Enter]를 누르세요.");
            Console.WriteLine("[Client] 종료하려면 'exit'를 입력하세요.\n");

            while (true)
            {
                Console.Write("Input > ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                // byte[] 조립 없이 객체로 바로 송신
                client.SendPacket(new C_EchoRequest { Message = input });
            }

            Console.WriteLine("[Client] 서버와 연결을 해제하고 종료합니다.");
            client.Disconnect();
        }
    }
}
