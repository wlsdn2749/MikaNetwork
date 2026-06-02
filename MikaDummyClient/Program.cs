using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MikaServerCore;
using MikaServerCore.Network;

namespace MikaDummyClient
{
    class Program
    {
        private static async Task Main(String[] args)
        {
            var client = new MikaClient();
            var packetManager = new PacketManager();

            client.PacketReceived += (session, data) =>
            {
                packetManager.OnRecvPacket(session, data);

                return ValueTask.CompletedTask;
            };
            
            await client.ConnectAsync("127.0.0.1", 10010);
            
            // 연결 유지 및 키 입력 대기 (CPU 점유 없음)
            Console.WriteLine("[Client] 보낼 메시지를 입력하고 [Enter]를 누르세요.");
            Console.WriteLine("[Client] 종료하려면 'exit'를 입력하세요.\n");
            // 3. 입력 루프 시작
            while (true)
            {
                Console.Write("Input > ");
                string? input = Console.ReadLine();
                // 입력이 비어있으면 다시 대기
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                // 'exit' 입력 시 연결 종료 후 대화 루프 탈출
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                // 4. 입력 문자열을 UTF-8 바이트배열로 변환
                byte[] body = Encoding.UTF8.GetBytes(input);
                // 5. C_EchoRequest(ID: 1) 패킷 조립
                byte[] packet = MikaPacketBuilder.MakePacket((ushort)PacketId.C_EchoRequest, body);
                // 6. 서버로 송신
                client.Send(packet);
            }
            // 7. 종료 처리
            Console.WriteLine("[Client] 서버와 연결을 해제하고 종료합니다.");
            client.Disconnect();
        }
    }
}
