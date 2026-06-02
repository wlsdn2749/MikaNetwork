namespace MikaServerCore.Network;

/// <summary>
/// 1. 패킷은 반드시 ushort인 id, size를 포함해야 함.
/// 2. ([id][size][---body---]) 이렇게 이루어진 byte array를 TCP로 송수신 함
/// 3. id, size는 먼저 body를 serialize한 후, size를 측정하여 앞 비트에 써넣는 방식을 사용하며 
/// 4. body부분은 MemoryPack 등으로 Serialize/Deserialize 한다.
/// </summary>
///

// public readonly struct PacketHeader(ushort id, ushort size);
public enum PacketId : ushort
{
    None = 0,
    C_EchoRequest = 1,
    S_EchoResponse = 2
}

public class MikaPacketBuilder
{
    private const int HeaderSize = sizeof(ushort) + sizeof(ushort);
    private const int MaxBodySize = 4096;
    
    public MikaPacketBuilder()
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