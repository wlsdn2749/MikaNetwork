using MikaProtocol;
using MikaServerCore.Network;

namespace MikaDummyClient;

public static class ServerPacketHandler
{
    [PacketHandler]
    public static void Handle_S_EchoResponse(MikaSession session, S_EchoResponse res)
    {
        Console.WriteLine($"[Client] Recv Echo: {res.Message}");
    }
}
