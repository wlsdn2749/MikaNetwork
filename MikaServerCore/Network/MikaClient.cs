using System.Net;

namespace MikaServerCore.Network;

public sealed class MikaClient
{
    private readonly MikaConnector _connector = new();
    public MikaSession? Session { get; private set; }
    public event Func<MikaSession, ReadOnlyMemory<byte>, ValueTask>? PacketReceived;

    public async Task ConnectAsync(string ip, int port) => await ConnectAsync(IPAddress.Parse(ip), port);
    public async Task ConnectAsync(IPAddress ipAddress, int port)
    {
        Session = await _connector.ConnectAsync(ipAddress, port);

        Session.Received += OnSessionPacketReceived;
        Session.Connected += OnConnected;
        Session.Disconnected += OnDisconnected;

        _ = Session.StartAsync();
    }

    public void Disconnect()
    {
        Session?.Disconnect();
    }

    public void Send(byte[] data)
    {
        Session?.Send(data);
    }

    private async ValueTask OnSessionPacketReceived(MikaSession session, ReadOnlyMemory<byte> data)
    {
        var handler = PacketReceived;
        if (handler != null)
        {
            await handler(session, data);
        }
    }
    
    
    private void OnConnected(MikaSession session)
    {
        Console.WriteLine($"Connected to {session.RemoteEndPoint}");
    }

    private void OnDisconnected(MikaSession session)
    {
        Console.WriteLine($"$Disconnected from {session.RemoteEndPoint}");
    }
    
}