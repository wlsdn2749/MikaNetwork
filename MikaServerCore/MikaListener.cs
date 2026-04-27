using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MikaServerCore;

public class MikaListener : IDisposable
{
    private readonly IPEndPoint _listenSocketEP;
    private Socket _listenSocket;
    private int _backlog = 10000;
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
    
    public void Start()
    {
        _listenSocket.Bind(_listenSocketEP);
        _listenSocket.Listen(_backlog);

        _active = true;
    }

    public async Task<Socket> AcceptAsync()
    {
        var acceptSocket = await _listenSocket.AcceptAsync();
        return acceptSocket;
    }

    public void Dispose()
    {
        _listenSocket.Dispose();
    }
}