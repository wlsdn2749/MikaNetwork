using MikaUtils;
using MikaProtocol;
using MikaServerCore.Network;

namespace MikaDummyServer;

public class NetworkManager : Singleton<NetworkManager>
{
    public void Initialize()
    {
        var server = new MikaServer(10010);
        var packetManager = new MikaPacketManager();
        packetManager.Register<C_EchoRequest>((ushort)PacketId.C_EchoRequest, PacketHandler.Handle_C_EchoRequest);

        server.PacketReceived += (session, data) =>
        {
            packetManager.OnRecvPacket(session, data);
            return ValueTask.CompletedTask;
        };

        server.Listen();
    }
}