namespace MikaServerCore.Network;

public static class MikaPacketBuilder
{
    public const int HeaderSize = sizeof(ushort) + sizeof(ushort);
    public const int MaxPacketSize = 4096;
    
    static MikaPacketBuilder()
    {
        
    }
    
    public static ushort ReadId(ReadOnlySpan<byte> packet) => BitConverter.ToUInt16(packet);
    public static ushort ReadSize(ReadOnlySpan<byte> packet) => BitConverter.ToUInt16(packet.Slice(sizeof(ushort)));
    public static ReadOnlyMemory<byte> ReadBody(ReadOnlyMemory<byte> packet) => packet.Slice(HeaderSize);

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