using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MikaServerCore;

public class MikaListener : IDisposable
{
    private readonly IPEndPoint _listenSocketEP;
    private Socket _listenSocket;
    private bool _active;

    public EndPoint EndPoint => _active ? _listenSocket.LocalEndPoint! : _listenSocketEP;

    public MikaListener(string ipAddress, int port) 
        : this(IPAddress.Parse(ipAddress), port)
    {
        
    }
    public MikaListener(IPAddress ipAddress, int port)
    {
        _listenSocketEP = new IPEndPoint(ipAddress, port);
        _listenSocket   = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }
    
    public async Task Start()
    {
        _listenSocket.Bind(_listenSocketEP);
        _listenSocket.Listen((int)SocketOptionName.MaxConnections);

        _active = true;

        while (true)
        {
            var acceptClient = await _listenSocket.AcceptAsync();

            _ = HandleClientAsync(acceptClient);
        }
    }


    private async Task HandleClientAsync(Socket acceptClient)
    {
        await using NetworkStream stream = new NetworkStream(acceptClient, ownsSocket: true);

        byte[] buffer = new byte[4096];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer);

                if (bytesRead == 0)
                {
                    break;
                }

                ReadOnlyMemory<byte> received = buffer.AsMemory(0, bytesRead);
            }

        }
        catch (Exception ex)
        {
            
        }
    }
    public void Dispose()
    {
        _listenSocket.Dispose();
    }
}