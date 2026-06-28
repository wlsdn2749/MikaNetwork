using MikaNetwork;

namespace MikaDummyServer.Network;

public class ClientPacketManager : MikaPacketManager
{
    public ClientPacketManager()
    {
        MikaGenerated.GeneratedHandlers.RegisterAll(this);
    }
}
