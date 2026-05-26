using System.Net;

namespace MikaServerCore.Network;

public class MikaServer : IDisposable
{
    private readonly MikaAcceptor _acceptor;
    private readonly PacketManager _packetManager = new();
    public SessionManager SessionManager { get; init; } = new();
    
    
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
        session.Received += OnReceived;
        
        SessionManager.TryAdd(session.SessionId, session);

        _ = session.StartAsync();

    }

    private ValueTask OnReceived(MikaSession session, ReadOnlyMemory<byte> data)
    {
        _packetManager.OnRecvPacket(session, data);
        return ValueTask.CompletedTask;
    }
    private void OnDisconnected(MikaSession session)
    {
        SessionManager.TryRemove(session.SessionId, out _);
    }
}