using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore;

public class Listener
{
    private Socket _listenSocket = null!;

    public void Init(IPEndPoint endpoint)
    {
        _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(endpoint);
        _listenSocket.Listen(100);
    }

    public async Task StartAcceptAsync()
    {
        var handler = await _listenSocket.AcceptAsync();
        Console.WriteLine("New Client Connected");

        _ = HandleClientAsync(handler);
    }

    public async Task HandleClientAsync(Socket handler)
    {
        byte[] sizeBuffer = new byte[4];
        
        await handler.ReceiveAsync(sizeBuffer, SocketFlags.None);
        int dataSize = BitConverter.ToInt32(sizeBuffer, 0);
        
        byte[] bodyBuffer = new byte[dataSize];
        int totalRead = 0;

        while (totalRead < dataSize)
        {
            int read = await handler.ReceiveAsync(bodyBuffer.AsMemory().Slice(totalRead), SocketFlags.None);
            totalRead += read;
        }

        string message = Encoding.UTF8.GetString(bodyBuffer);
        
        Console.WriteLine($"Socket server sent message: \"{message}\"");
    }
}