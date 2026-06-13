using MikaProtocol;
using MikaServerCore.Network;

namespace MikaDummyServer;

public class ClientPacketManager : MikaPacketManager
{
    public ClientPacketManager()
    {
        MikaGenerated.GeneratedHandlers.RegisterAll(this);
    }
}
