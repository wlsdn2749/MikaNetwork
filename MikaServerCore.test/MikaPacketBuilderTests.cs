using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

public class MikaPacketBuilderTests
{
    private const int HeaderSize = 4; // [id:2][size:2]

    [Fact]
    public void MakePacket_Layout_Is_Id_Size_Body()
    {
        byte[] body = [0xAA, 0xBB, 0xCC];

        byte[] packet = MikaPacketBuilder.MakePacket(7, body);

        packet.Length.ShouldBe(HeaderSize + body.Length);
        BitConverter.ToUInt16(packet, 0).ShouldBe((ushort)7);                       // [0..2) = id
        BitConverter.ToUInt16(packet, 2).ShouldBe((ushort)(HeaderSize + body.Length)); // [2..4) = size(헤더 포함)
        packet[HeaderSize..].ShouldBe(body);
    }

    [Fact]
    public void MakePacket_With_Empty_Body_Is_Header_Only()
    {
        byte[] packet = MikaPacketBuilder.MakePacket(1, []);

        packet.Length.ShouldBe(HeaderSize);
        BitConverter.ToUInt16(packet, 2).ShouldBe((ushort)HeaderSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(ushort.MaxValue)]
    public void MakePacket_Writes_Any_PacketId(ushort id)
    {
        byte[] packet = MikaPacketBuilder.MakePacket(id, [1, 2]);

        BitConverter.ToUInt16(packet, 0).ShouldBe(id);
    }
}
