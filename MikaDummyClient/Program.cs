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
            await client.ConnectAsync("127.0.0.1", 10010);
            
            // 연결 유지 및 키 입력 대기 (CPU 점유 없음)
            Console.WriteLine("[Client] 종료하려면 엔터를 누르세요.");
            Console.ReadLine();
        }
    }
}
