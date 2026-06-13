using MikaUtils;
using MikaServerCore.Network;

namespace MikaDummyServer;

public class NetworkManager : Singleton<NetworkManager>
{
    private readonly MikaServer _server = new(10010);
    private readonly ClientPacketManager _packetManager = new();

    public void Initialize()
    {
        _server.PacketReceived += (session, data) =>
        {
            _packetManager.OnRecvPacket(session, data);
            return ValueTask.CompletedTask;
        };

        _server.Listen();
    }
}
