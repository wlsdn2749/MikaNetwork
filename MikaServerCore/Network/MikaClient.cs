using System.Net;

namespace MikaServerCore.Network;

public sealed class MikaClient
{
    private readonly MikaConnector _connector = new();
    public MikaSession? Session { get; private set; }

    public async Task ConnectAsync(string ip, int port) => await ConnectAsync(IPAddress.Parse(ip), port);
    public async Task ConnectAsync(IPAddress ipAddress, int port)
    {
        Session = await _connector.ConnectAsync(ipAddress, port);

        Session.Received += OnReceived;
        Session.Disconnected += OnDisconnected;

        _ = Session.StartAsync();
    }

    public void Disconnect()
    {
        Session?.Disconnect();
    }

    private ValueTask OnReceived(MikaSession session, ReadOnlyMemory<byte> data)
    {
        return ValueTask.CompletedTask;
    }
    
    
    private void OnConnected(MikaSession session)
    {
        
    }

    private void OnDisconnected(MikaSession session)
    {
        
    }
}