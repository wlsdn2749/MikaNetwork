using System.Net;
using System.Net.Sockets;

using MikaServerCore.Interface;
using MikaServerCore.Package.Text;

namespace MikaServerCore.Network;

public class Server
{
    // Listener that owns the TCP accept loop.
    private readonly Listener _listener = new();

    // Endpoint configured by Init.
    // StartAsync validates this value before binding listener socket.
    private IPEndPoint? _endpoint;

    // Active sessions managed by this server instance.
    // Kept for lifecycle tracking and future features like broadcast.
    private readonly List<Session> _sessions = [];

    // Synchronizes access to _sessions because accept and disconnect run concurrently.
    private readonly object _sessionsLock = new();
    
    // Handler that executes business logic for decoded text packages.
    private readonly IPackageHandler<TextPackageInfo> _packageHandler;

    // Session-level pipeline filter to use when creating new sessions.
    // This allows selecting decoder strategy without changing Session internals.
    private readonly IPipelineFilter<TextPackageInfo> _pipelineFilter;

    // Hook called after a client has connected and session is created.
    public Action<Session>? OnClientConnected;

    // Hook called when a client session has ended and is removed.
    public Action<Session>? OnClientDisconnected;

    // Construct server with explicit endpoint, while still supporting separate Init calls.
    // This keeps constructor-based usage and Init-style usage both available.
    public Server(
        IPEndPoint endpoint,
        IPackageHandler<TextPackageInfo> handler,
        IPipelineFilter<TextPackageInfo>? pipelineFilter = null)
    {
        _endpoint = endpoint;
        _packageHandler = handler;
        _pipelineFilter = pipelineFilter ?? new LinePipelineFilter();
    }

    // Reconfigure endpoint after construction when caller wants explicit two-step setup.
    public void Init()
    {
        // 여기서는 뭘해야할까..?
    }

    // Start server on endpoint prepared by Init and enter accept loop.
    public async Task StartAsync()
    {
        // Fail fast when Init was not called.
        // This prevents null endpoint usage and makes startup mistakes explicit.
        if (_endpoint is null)
            throw new InvalidOperationException("Server is not initialized. Call Init(IPEndPoint endpoint) before StartAsync().");

        _listener.Init(_endpoint);
        _listener.OnAcceptHandler = HandleAcceptedSocket;

        Console.WriteLine($"Listening on {_endpoint}");
        await _listener.StartAcceptAsync();
    }

    // Create and start a session from an accepted socket.
    private void HandleAcceptedSocket(Socket socket)
    {
        // Create each session with configured pipeline filter.
        // This is the key point where filter selection is applied.
        var session = new Session(_pipelineFilter);
        session.Init(socket);
        session.OnPackageReceived = HandleReceivedPackage;

        lock (_sessionsLock)
        {
            _sessions.Add(session);
        }

        OnClientConnected?.Invoke(session);

        // Run each session independently from accept loop.
        _ = RunSessionAsync(session);
    }

    // Forward package to external async business handler.
    private void HandleReceivedPackage(Session session, TextPackageInfo package)
    {
        _ = _packageHandler.HandleAsync(session, package);
    }

    // Run session receive loop and always clean up server-side tracking.
    private async Task RunSessionAsync(Session session)
    {
        try
        {
            await session.StartAsync();
        }
        finally
        {
            lock (_sessionsLock)
            {
                _sessions.Remove(session);
            }

            OnClientDisconnected?.Invoke(session);
        }
    }
}
