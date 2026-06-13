using System.Net.Sockets;
using MemoryPack;
using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

[MemoryPackable]
public partial class TestEchoPacket
{
    public string Message { get; set; } = "";
}

public class MikaPacketManagerTests
{
    // PacketManager는 세션을 핸들러로 넘기기만 하므로 연결 안 된 더미 세션이면 충분하다
    private static MikaSession CreateDummySession()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        return MikaSessionFactory.Create(socket);
    }

    private static byte[] MakeFramedPacket<T>(ushort id, T packet)
    {
        byte[] body = MemoryPackSerializer.Serialize(packet);
        return MikaPacketBuilder.MakePacket(id, body);
    }

    /// <summary>등록한 핸들러가 올바른 타입으로 역직렬화된 패킷과 원본 세션을 그대로 전달받는지 확인한다.</summary>
    [Fact]
    public void Registered_Handler_Receives_Deserialized_Packet()
    {
        var manager = new MikaPacketManager();
        var session = CreateDummySession();
        TestEchoPacket? received = null;
        MikaSession? receivedSession = null;

        manager.Register<TestEchoPacket>(1, (s, packet) =>
        {
            receivedSession = s;
            received = packet;
        });

        byte[] framed = MakeFramedPacket(1, new TestEchoPacket { Message = "hello" });
        manager.OnRecvPacket(session, framed);

        received.ShouldNotBeNull();
        received.Message.ShouldBe("hello");
        receivedSession.ShouldBeSameAs(session);
    }

    /// <summary>등록되지 않은 PacketId가 오면 예외 없이 조용히 무시되는지 확인한다.</summary>
    [Fact]
    public void Unknown_PacketId_Is_Ignored()
    {
        var manager = new MikaPacketManager();
        bool called = false;
        manager.Register<TestEchoPacket>(1, (_, _) => called = true);

        byte[] framed = MakeFramedPacket(999, new TestEchoPacket { Message = "x" });

        Should.NotThrow(() => manager.OnRecvPacket(CreateDummySession(), framed));
        called.ShouldBeFalse();
    }

    /// <summary>헤더(4바이트)보다 짧은 패킷이 오면 예외 없이 무시되는지 확인한다.</summary>
    [Fact]
    public void Packet_Shorter_Than_Header_Is_Ignored()
    {
        var manager = new MikaPacketManager();
        bool called = false;
        manager.Register<TestEchoPacket>(1, (_, _) => called = true);

        Should.NotThrow(() => manager.OnRecvPacket(CreateDummySession(), new byte[] { 1, 0, 3 }));
        called.ShouldBeFalse();
    }

    /// <summary>같은 PacketId로 두 번 등록하면 나중 등록이 이전 핸들러를 덮어쓰는지 확인한다.</summary>
    [Fact]
    public void Last_Registration_Wins_For_Same_Id()
    {
        var manager = new MikaPacketManager();
        string? handledBy = null;
        manager.Register<TestEchoPacket>(1, (_, _) => handledBy = "first");
        manager.Register<TestEchoPacket>(1, (_, _) => handledBy = "second");

        manager.OnRecvPacket(CreateDummySession(), MakeFramedPacket(1, new TestEchoPacket()));

        handledBy.ShouldBe("second");
    }
}
