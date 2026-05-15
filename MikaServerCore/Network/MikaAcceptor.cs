using System.Net;
using System.Net.Sockets;

namespace MikaServerCore.Network;

public class MikaAcceptor : IDisposable
{
    private readonly IPEndPoint _listenSocketEp;
    private readonly Socket? _listenSocket;
    private bool _active;

    public EndPoint EndPoint => _active ? _listenSocket?.LocalEndPoint! : _listenSocketEp;

    public event Action<MikaSession>? Accepted;

    public MikaAcceptor(string ipAddress, int port) 
        : this(new MikaAcceptorOptions{LocalAddr = IPAddress.Parse(ipAddress), Port = port})
    {
        
    }

    public MikaAcceptor(IPAddress ipAddress, int port)
        : this(new MikaAcceptorOptions{LocalAddr = ipAddress, Port = port})
    {
        
    }

    public MikaAcceptor(MikaAcceptorOptions options)
    {
        _listenSocketEp = new IPEndPoint(options.LocalAddr, options.Port);
        _listenSocket   = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public void Listen()
    {
        _listenSocket?.Bind(_listenSocketEp);
        _listenSocket?.Listen((int)SocketOptionName.MaxConnections);

        _active = true;

        _ = AcceptLoopAsync();
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            var client = await _listenSocket?.AcceptAsync();
            
            var session = MikaSessionFactory.Create(client);
            
            Accepted?.Invoke(session);
        }
    }
    

    // private async Task HandleClientAsync(Socket acceptClient)
    // {
    //     await using NetworkStream stream = new NetworkStream(acceptClient, ownsSocket: true);
    //
    //     byte[] buffer = new byte[4096];
    //
    //     try
    //     {
    //         while (true)
    //         {
    //             int bytesRead = await stream.ReadAsync(buffer);
    //
    //             if (bytesRead == 0)
    //             {
    //                 break;
    //             }
    //
    //             ReadOnlyMemory<byte> received = buffer.AsMemory(0, bytesRead);
    //         }
    //
    //     }
    //     catch (Exception ex)
    //     {
    //         
    //     }
    // }
    public void Dispose()
    {
        _listenSocket?.Dispose();
    }
}