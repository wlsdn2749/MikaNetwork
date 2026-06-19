using System.Net;
using MemoryPack;
using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

/// <summary>
/// 클라이언트 패킷 송신 → 서버 디스패치 → 응답 → 클라이언트 수신까지
/// 전송/프레이밍/디스패치 전 스택을 관통하는 에코 테스트.
/// </summary>
public class MikaEchoEndToEndTests
{
    private const ushort EchoRequestId = 1;
    private const ushort EchoResponseId = 2;

    private static byte[] MakeFramedPacket<T>(ushort id, T packet)
    {
        return MikaPacketBuilder.MakePacket(id, MemoryPackSerializer.Serialize(packet));
    }

    /// <summary>클라이언트 송신 → 서버 디스패치 → 응답 → 클라이언트 수신까지 전 스택을 관통해 같은 메시지가 그대로 에코되는지 확인한다.</summary>
    [Theory]
    [InlineData("Hello Mika!")]
    [InlineData("안녕하세요")]
    [InlineData("")]
    public async Task Client_Sends_Echo_And_Receives_Same_Message(string message)
    {
        using var server = new LoopbackServer();

        // 서버: EchoRequest를 받으면 같은 메시지를 EchoResponse로 회신
        var serverPackets = new MikaPacketManager();
        serverPackets.Register<TestEchoPacket>(EchoRequestId, (session, packet) =>
        {
            session.Send(MakeFramedPacket(EchoResponseId, packet));
        });
        server.Server.PacketReceived += (session, data) =>
        {
            serverPackets.OnRecvPacket(session, data);
            return ValueTask.CompletedTask;
        };

        // 클라이언트: EchoResponse 수신 시 완료
        var responseReceived = new TaskCompletionSource<TestEchoPacket>();
        var clientPackets = new MikaPacketManager();
        clientPackets.Register<TestEchoPacket>(EchoResponseId, (_, packet) =>
        {
            responseReceived.TrySetResult(packet);
        });

        using var connector = new MikaConnector();
        var session = await connector.ConnectAsync(IPAddress.Loopback, server.Port);
        session.Received += (s, data) =>
        {
            clientPackets.OnRecvPacket(s, data);
            return ValueTask.CompletedTask;
        };
        _ = session.StartAsync();

        session.Send(MakeFramedPacket(EchoRequestId, new TestEchoPacket { Message = message }));

        var response = await responseReceived.Task.WaitAsync(TimeSpan.FromSeconds(2));
        response.Message.ShouldBe(message);
    }
}
