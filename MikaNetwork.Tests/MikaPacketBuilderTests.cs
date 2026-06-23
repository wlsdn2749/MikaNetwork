using MikaNetwork.Core.Network;
using Shouldly;

namespace MikaServerCore.test;

public class MikaPacketBuilderTests
{
    private const int HeaderSize = 4; // [id:2][size:2]

    /// <summary>MakePacket 결과가 [id:2][size:2][body] 레이아웃을 따르고, size가 헤더를 포함한 전체 길이인지 확인한다.</summary>
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

    /// <summary>바디가 비어 있으면 헤더만 있는 패킷이 만들어지고 size가 헤더 크기와 같은지 확인한다.</summary>
    [Fact]
    public void MakePacket_With_Empty_Body_Is_Header_Only()
    {
        byte[] packet = MikaPacketBuilder.MakePacket(1, []);

        packet.Length.ShouldBe(HeaderSize);
        BitConverter.ToUInt16(packet, 2).ShouldBe((ushort)HeaderSize);
    }

    /// <summary>0·1·ushort 최댓값 등 어떤 PacketId든 헤더 앞 2바이트에 정확히 기록되는지(경계값) 확인한다.</summary>
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
