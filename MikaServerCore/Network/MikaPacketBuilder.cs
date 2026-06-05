namespace MikaServerCore.Network;

public static class MikaPacketBuilder
{
    private const int HeaderSize = sizeof(ushort) + sizeof(ushort);
    private const int MaxBodySize = 4096;
    
    static MikaPacketBuilder()
    {
        
    }

    public static byte[] MakePacket(ushort packetId, byte[] body)
    {
        byte[] packet = new byte[HeaderSize + body.Length];
        ushort size = (ushort)(HeaderSize + body.Length);

        body.CopyTo(packet, HeaderSize); // 직렬화된 바디 복사... HeaderSize만큼 빼고
        BitConverter.TryWriteBytes(packet.AsSpan(0, sizeof(ushort)), packetId);
        BitConverter.TryWriteBytes(packet.AsSpan(sizeof(ushort), sizeof(ushort)), size);

        return packet;
    }
}