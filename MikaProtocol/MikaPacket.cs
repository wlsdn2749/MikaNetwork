using MikaProtocol.Interfaces;
using MemoryPack;

namespace MikaProtocol;

/// <summary>
/// 1. 패킷은 반드시 ushort인 id, size를 포함해야 함.
/// 2. ([id][size][---body---]) 이렇게 이루어진 byte array를 TCP로 송수신 함
/// 3. id, size는 먼저 body를 serialize한 후, size를 측정하여 앞 비트에 써넣는 방식을 사용하며 
/// 4. body부분은 MemoryPack 등으로 Serialize/Deserialize 한다.
/// </summary>
///

public enum PacketId : ushort
{
    None = 0,
    C_EchoRequest = 1,
    S_EchoResponse = 2
}

[MemoryPackable]
public partial class C_EchoRequest : IPacket
{
    public PacketId Id => PacketId.C_EchoRequest;

    public string Message { get; set; } = "";
}

[MemoryPackable]
public partial class S_EchoResponse : IPacket
{
    public PacketId Id => PacketId.S_EchoResponse;
    
    public string Message { get; set; } = "";
}