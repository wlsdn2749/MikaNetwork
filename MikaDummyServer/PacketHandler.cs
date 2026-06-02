using MikaServerCore.Network;

namespace MikaDummyServer;

public class PacketHandler
{
    public static void Handle_C_EchoRequest(MikaSession session, ReadOnlyMemory<byte> data)
    {
        Console.WriteLine($"Received EchoRequest: {data}");
         
        byte[] packet = MikaPacketBuilder.MakePacket((ushort)PacketId.S_EchoResponse, data.ToArray());
        session.Send(packet);
    }
}