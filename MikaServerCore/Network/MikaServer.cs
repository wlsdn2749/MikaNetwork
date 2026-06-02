using System.Net;

namespace MikaServerCore.Network;

public class MikaServer : IDisposable
{
    private readonly MikaAcceptor _acceptor;
    public SessionManager SessionManager { get; init; } = new();

    public event Func<MikaSession, ReadOnlyMemory<byte>, ValueTask>? PacketReceived;
    public EndPoint EndPoint => _acceptor.EndPoint;
    
    public MikaServer(int port)
        : this(new MikaServerOptions
        {
            AcceptorOpt = new MikaAcceptorOptions { LocalAddr = IPAddress.Any, Port = port }
        })
    {
        
    }
    public MikaServer(IPAddress address, int port)
        : this(new MikaServerOptions
        {
            AcceptorOpt = new MikaAcceptorOptions { LocalAddr = address, Port = port }
        })
    {
        
    }
    public MikaServer(MikaServerOptions options)
    {
        _acceptor = new MikaAcceptor(options.AcceptorOpt);
        _acceptor.Accepted += OnAccepted;
        
    }

    public void Listen()
    {
        _acceptor.Listen();
    }


    public void Send(int sessionId, byte[] data)
    {
        if (SessionManager.TryGet(sessionId, out var session))
        {
            session?.Send(data);
        }
    }
    public void Send(MikaSession session, byte[] data)
    {
        session.Send(data);    
    }

    public void Stop()
    {
        Dispose();
    }
    
    public void Dispose()
    {
        _acceptor.Dispose();
    }

    private void OnAccepted(MikaSession session)
    {
        session.Disconnected += OnDisconnected;
        session.Received += OnSessionPacketReceived;
        
        SessionManager.TryAdd(session.SessionId, session);
        
        Console.WriteLine($"[{session.SessionId}] Accepted");

        _ = session.StartAsync();
    }
    private void OnDisconnected(MikaSession session)
    {
        session.Received -= OnSessionPacketReceived;
        
        SessionManager.TryRemove(session.SessionId, out _);
    }

    private async ValueTask OnSessionPacketReceived(MikaSession session, ReadOnlyMemory<byte> data)
    {
        var handler = PacketReceived;
        if (handler != null)
        {
            await handler(session, data);
        }
    }
    
    
}