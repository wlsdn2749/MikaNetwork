using MikaServerCore.Network;

namespace MikaDummyClient;

public class PacketHandler
{
    public static void Handle_S_EchoResponse(MikaSession session, ReadOnlyMemory<byte> data)
    {
        Console.WriteLine($"Received EchoRequest: {data}");
    }
}