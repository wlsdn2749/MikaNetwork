using MikaProtocol;
using MikaProtocol.Interfaces;
using MikaServerCore.Network;
using MikaUtils;

namespace MikaDummyClient;

public class NetworkManager : Singleton<NetworkManager>
{
    private MikaClient _client;
    
    public async Task Initialize()
    {
        _client = new MikaClient();
        
        var packetManager = new MikaPacketManager();
        packetManager.Register<S_EchoResponse>((ushort)PacketId.S_EchoResponse, PacketHandler.Handle_S_EchoResponse);

        _client.PacketReceived += (session, data) =>
        {
            packetManager.OnRecvPacket(session, data);
            return ValueTask.CompletedTask;
        };
        
        await _client.ConnectAsync("127.0.0.1", 10010);
    }

    public void Send(IPacket packet)
    {
        _client.SendPacket(packet);
    }
}