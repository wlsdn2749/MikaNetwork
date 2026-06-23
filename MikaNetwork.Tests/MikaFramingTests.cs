using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MikaNetwork.Core.Network;
using Shouldly;

namespace MikaServerCore.test;

/// <summary>
/// TCP 스트림 경계와 패킷 경계가 다를 때 ReceiveLoop가 [id][size][body]를
/// 올바르게 재조립하는지 검증한다. raw TcpClient로 바이트를 직접 쪼개/뭉쳐 보낸다.
/// </summary>
public class MikaFramingTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    private static (LoopbackServer server, ConcurrentQueue<byte[]> received) CreateCapturingServer()
    {
        var server = new LoopbackServer();
        var received = new ConcurrentQueue<byte[]>();
        server.Server.PacketReceived += (_, data) =>
        {
            received.Enqueue(data.ToArray());
            return ValueTask.CompletedTask;
        };
        return (server, received);
    }

    /// <summary>한 패킷이 여러 TCP 세그먼트로 쪼개져 도착할 때, 완성되기 전엔 전달되지 않다가 완성되면 정확히 1번 재조립되는지 확인한다.</summary>
    [Fact]
    public async Task Split_Packet_Is_Reassembled()
    {
        var (server, received) = CreateCapturingServer();
        using var _ = server;

        byte[] packet = MikaPacketBuilder.MakePacket(1, [10, 20, 30, 40, 50]);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        var stream = client.GetStream();

        // 헤더 일부(3바이트)만 먼저 → 잠시 후 나머지
        await stream.WriteAsync(packet.AsMemory(0, 3));
        await stream.FlushAsync();
        await Task.Delay(100);

        received.ShouldBeEmpty(); // 미완성 패킷은 아직 전달되면 안 됨

        await stream.WriteAsync(packet.AsMemory(3));
        await stream.FlushAsync();

        (await TestHelpers.WaitUntilAsync(() => received.Count == 1, Timeout)).ShouldBeTrue();
        received.TryDequeue(out var framed).ShouldBeTrue();
        framed.ShouldBe(packet);
    }

    /// <summary>여러 패킷이 한 번의 수신으로 뭉쳐 들어와도(sticky packet) 각각 개별 패킷으로 분리·디스패치되는지 확인한다.</summary>
    [Fact]
    public async Task Coalesced_Packets_Are_Dispatched_Individually()
    {
        var (server, received) = CreateCapturingServer();
        using var _ = server;

        byte[][] packets =
        [
            MikaPacketBuilder.MakePacket(1, [0xA]),
            MikaPacketBuilder.MakePacket(2, [0xB, 0xB]),
            MikaPacketBuilder.MakePacket(3, [0xC, 0xC, 0xC]),
        ];
        byte[] combined = packets.SelectMany(p => p).ToArray();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);

        // 패킷 3개를 한 번의 write로 전송 (sticky packet)
        await client.GetStream().WriteAsync(combined);

        (await TestHelpers.WaitUntilAsync(() => received.Count == 3, Timeout)).ShouldBeTrue();
        received.ToArray().ShouldBe(packets);
    }

    /// <summary>한 번의 수신 청크 크기를 넘는 큰 패킷이 여러 번의 Receive에 걸쳐 손실 없이 재조립되는지 확인한다.</summary>
    [Fact]
    public async Task Packet_Larger_Than_Recv_Chunk_Is_Reassembled()
    {
        var (server, received) = CreateCapturingServer();
        using var _ = server;

        // MikaSession.MaxPacketSize(4096)보다 큰 바디 → 여러 번의 Receive에 걸쳐 조립
        byte[] body = new byte[10_000];
        Random.Shared.NextBytes(body);
        byte[] packet = MikaPacketBuilder.MakePacket(1, body);

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        await client.GetStream().WriteAsync(packet);

        (await TestHelpers.WaitUntilAsync(() => received.Count == 1, Timeout)).ShouldBeTrue();
        received.TryDequeue(out var framed).ShouldBeTrue();
        framed.ShouldBe(packet);
    }

    /// <summary>size=0인 손상/악성 헤더가 와도 무한 루프에 빠지지 않고 프로토콜 위반으로 세션을 끊는지 확인한다.</summary>
    [Fact]
    public async Task Malformed_Size_Zero_Packet_Disconnects_Session()
    {
        var (server, received) = CreateCapturingServer();
        using var _ = server;

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, server.Port);
        (await TestHelpers.WaitUntilAsync(() => server.Server.SessionManager.Count == 1, Timeout))
            .ShouldBeTrue();

        // size=0인 손상/악성 헤더 — 0바이트씩 영원히 소비하는 무한 루프에 빠지면 안 되고,
        // 프로토콜 위반으로 보고 세션을 끊어야 한다
        byte[] malformed = [1, 0, 0, 0];
        await client.GetStream().WriteAsync(malformed);

        (await TestHelpers.WaitUntilAsync(() => server.Server.SessionManager.Count == 0, Timeout))
            .ShouldBeTrue();
        received.ShouldBeEmpty();
    }
}
