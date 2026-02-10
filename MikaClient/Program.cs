using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace MikaClient
{
    class Program
    {
        static async Task Main(String[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 8080;

            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            await client.ConnectAsync(ipEndPoint);
            
            while(true)
            {
                var message = "Hi friends abc";
                byte[] body = Encoding.UTF8.GetBytes(message); // 여기서 먼저 body로 byte[]를 만들고

                int dataSize = body.Length;
                
                var sendBuffer = new byte[4 + body.Length]; // header(4)

                Span<byte> span = sendBuffer;
                
                BitConverter.TryWriteBytes(span.Slice(0, 4), dataSize);
                
                body.AsSpan().CopyTo(span.Slice(4)); // span.Slice(4)에 나머지 추가
                
                _ = await client.SendAsync(sendBuffer, SocketFlags.None);
                Console.WriteLine($"Socket client sent message: \"{message}\"");
                

                //Receive ack.
                var buffer = new byte[1_024];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
                if (response == "<|ACK|>")
                {
                    Console.WriteLine($"Socket client received acknowledgment: \"{response}\"");
                    break;
                }

            }
            client.Shutdown(SocketShutdown.Both);
        }
    }
}