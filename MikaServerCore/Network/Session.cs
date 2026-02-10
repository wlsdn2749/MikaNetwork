using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore;

public class Session
{
    private Socket  _socket = null!;
    private int     _sessionId;
    
    public void Init(Socket socket)
    {
        _socket = socket;
    }
    public async Task<bool> ConnectAsync(IPEndPoint ipEndPoint)
    {
        try
        {
            _socket = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            await _socket.ConnectAsync(ipEndPoint);

            _ = StartAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connect Failed: {ex.Message}");
            return false;
        }
    }
    
    public async Task StartAsync()
    {
        // WhileLoop를 돌면서, ReceiveAsync 호출

        try
        {
            while (true)
            {
                // 1. 헤더 4바이트 먼저 읽기
                byte[] headerBuffer = new byte[4];
                int n1 = await _socket.ReceiveAsync(headerBuffer, SocketFlags.None);
                if (n1 == 0) break; // 상대방이 연결 끊음
                
                // 2. 바디 길이 파악
                int dataSize = BitConverter.ToInt32(headerBuffer, 0);
                
                // 3. 바디 사이즈만큼만 읽기
                byte[] bodyBuffer = new byte[dataSize];
                int totalRead = 0;
                
                while (totalRead < dataSize)
                {
                    int read = await _socket.ReceiveAsync(bodyBuffer.AsMemory().Slice(totalRead), SocketFlags.None);
                    totalRead += read;
                }
                
                OnReceive(bodyBuffer);
            }
        }
        catch (Exception ex)
        {
            
        }
    }

    public async Task SendAsync(byte[] buffer)
    {
        // Header를 붙여서 SendAsync 호출
        var sendBuffer = new byte[buffer.Length + 4];

        Span<byte> span = sendBuffer;
        BitConverter.TryWriteBytes(span.Slice(0, 4), buffer.Length);
        
        buffer.AsSpan().CopyTo(span.Slice(4));
        
        _ = await _socket.SendAsync(sendBuffer, SocketFlags.None);
        Console.WriteLine($"Sent {buffer.Length} bytes");
    }

    public virtual void OnReceive(byte[] buffer)
    {
        string message = Encoding.UTF8.GetString(buffer);
        Console.WriteLine($"Received {message}");
    }
    public virtual void OnDisconnected() {}

    public void Disconnect()
    {
        _socket?.Close();
    }
}