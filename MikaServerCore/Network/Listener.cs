using System.Net;
using System.Net.Sockets;

namespace MikaServerCore.Network;

public class Listener
{
    private Socket _listenSocket = null!;

    public void Init(IPEndPoint endpoint)
    {
        _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(endpoint);
        _listenSocket.Listen(100);
    }

    // 외부 주입 Method
    public Action<Socket>? OnAcceptHandler;

    public async Task StartAcceptAsync()
    {
        while (true)
        {
            var handler = await _listenSocket.AcceptAsync();
            Console.WriteLine("New Client Connected");

            OnAcceptHandler?.Invoke(handler);
        }
    }
}