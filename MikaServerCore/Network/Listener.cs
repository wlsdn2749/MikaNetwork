using System.Net;
using System.Net.Sockets;

namespace MikaServerCore.Network;

public class Listener
{
    // Server socket used by this listener.
    // It is created and bound in Init, then used by StartAcceptAsync.
    private Socket _listenSocket = null!;

    // Configure the listening socket for the given endpoint.
    // Backlog is set to 100 pending connections.
    public void Init(IPEndPoint endpoint)
    {
        _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(endpoint);
        _listenSocket.Listen(100);
    }

    // Callback invoked for every accepted client socket.
    // Higher-level code (Server) creates and starts a Session from this socket.
    public Action<Socket>? OnAcceptHandler;

    // Continuous accept loop.
    // Each incoming connection is accepted and forwarded to OnAcceptHandler.
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