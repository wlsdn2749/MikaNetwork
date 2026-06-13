using MikaProtocol;
using MikaServerCore.Network;
using MikaUtils;

namespace MikaDummyClient;

public class ServerPacketManager : MikaPacketManager
{
    public ServerPacketManager()
    {
        MikaGenerated.GeneratedHandlers.RegisterAll(this);
    }
}