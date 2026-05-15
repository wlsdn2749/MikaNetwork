using System.Net;

namespace MikaServerCore.Network;

public class MikaServer : IDisposable
{
    private readonly MikaAcceptor _acceptor;
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
        
        SessionManager.TryAdd(session.SessionId, session);

        _ = session.StartAsync();

    }

    private void OnDisconnected(MikaSession session)
    {
        SessionManager.TryRemove(session.SessionId, out _);
    }
}