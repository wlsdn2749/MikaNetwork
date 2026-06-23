using System.Net;
using MikaNetwork.Core.Interfaces;
using MikaNetwork.Core.Network;

namespace MikaNetwork.Server;

public class MikaServer : IDisposable
{
    private readonly MikaAcceptor _acceptor;
    public SessionManager SessionManager { get; init; } = new();

    public event Func<ISession, ReadOnlyMemory<byte>, ValueTask>? PacketReceived;
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
    public void Send(MikaServerSession serverSession, byte[] data)
    {
        serverSession.Send(data);    
    }

    public void Stop()
    {
        Dispose();
    }
    
    public void Dispose()
    {
        _acceptor.Dispose();
    }

    private void OnAccepted(MikaServerSession serverSession)
    {
        serverSession.Disconnected += OnDisconnected;
        serverSession.Received += OnSessionPacketReceived;
        
        SessionManager.TryAdd(serverSession.SessionId, serverSession);
        
        Console.WriteLine($"[{serverSession.SessionId}] Accepted");

        _ = serverSession.StartAsync();
    }
    private void OnDisconnected(ISession session)
    {
        session.Received -= OnSessionPacketReceived;
        
        SessionManager.TryRemove(session.SessionId, out _);
    }

    private async ValueTask OnSessionPacketReceived(ISession session, ReadOnlyMemory<byte> data)
    {
        var handler = PacketReceived;
        if (handler != null)
        {
            await handler(session, data);
        }
    }
    
    
}