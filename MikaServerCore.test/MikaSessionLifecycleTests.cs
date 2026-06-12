using System.Net;
using System.Net.Sockets;
using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

public class MikaSessionLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Connected_And_Disconnected_Fire_Exactly_Once()
    {
        using var server = new LoopbackServer();

        using var connector = new MikaConnector();
        var session = await connector.ConnectAsync(IPAddress.Loopback, server.Port);

        int connectedCount = 0;
        int disconnectedCount = 0;
        session.Connected += _ => Interlocked.Increment(ref connectedCount);
        session.Disconnected += _ => Interlocked.Increment(ref disconnectedCount);

        var startTask = session.StartAsync();
        (await TestHelpers.WaitUntilAsync(() => connectedCount == 1, Timeout)).ShouldBeTrue();

        session.Disconnect();

        // Disconnect는 ReceiveLoop 종료와 StartAsync의 finally 양쪽에서 불릴 수 있으므로
        // 이벤트가 중복 발화하지 않는지 잠시 기다린 후 확인한다
        await Task.Delay(200);
        connectedCount.ShouldBe(1);
        disconnectedCount.ShouldBe(1);
    }

    [Fact]
    public async Task Client_Detects_ServerSide_Disconnect()
    {
        using var server = new LoopbackServer();

        using var connector = new MikaConnector();
        var clientSession = await connector.ConnectAsync(IPAddress.Loopback, server.Port);

        int disconnectedCount = 0;
        clientSession.Disconnected += _ => Interlocked.Increment(ref disconnectedCount);
        _ = clientSession.StartAsync();

        (await TestHelpers.WaitUntilAsync(() => server.Server.SessionManager.Count == 1, Timeout))
            .ShouldBeTrue();

        // 서버 쪽에서 세션을 끊으면 클라이언트가 감지해야 한다
        var serverSession = server.Server.SessionManager.Sessions.Values.Single();
        serverSession.Disconnect();

        (await TestHelpers.WaitUntilAsync(() => disconnectedCount == 1, Timeout)).ShouldBeTrue();
        clientSession.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_Completes_After_Disconnect()
    {
        using var server = new LoopbackServer();

        using var connector = new MikaConnector();
        var session = await connector.ConnectAsync(IPAddress.Loopback, server.Port);

        var startTask = session.StartAsync();
        (await TestHelpers.WaitUntilAsync(() => session.IsConnected, Timeout)).ShouldBeTrue();

        session.Disconnect();

        // SendLoop까지 종료되어 StartAsync 태스크가 완료(누수 없이 회수)되어야 한다
        var completed = await Task.WhenAny(startTask, Task.Delay(Timeout));
        completed.ShouldBe(startTask);
    }

    [Fact]
    public async Task Send_Under_Backpressure_Does_Not_Drop_Data()
    {
        // 상대가 읽지 않는 동안 SendQueue(1024)보다 많이 보내도 데이터가 유실되면 안 된다
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var connector = new MikaConnector();
        var session = await connector.ConnectAsync(IPAddress.Loopback, port);
        _ = session.StartAsync();

        using var peer = await listener.AcceptTcpClientAsync().WaitAsync(Timeout);

        const int packetCount = 3000;
        const int packetSize = 4096;
        var payload = new byte[packetSize];

        for (int i = 0; i < packetCount; i++)
        {
            session.Send(payload); // peer는 아직 읽지 않음 → 소켓 버퍼/큐가 가득 참
        }

        // 이제 peer가 전부 읽는다 — 보낸 바이트가 전부 도착해야 한다
        long expected = (long)packetCount * packetSize;
        long total = 0;
        var stream = peer.GetStream();
        var buffer = new byte[64 * 1024];

        while (total < expected)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            int read;
            try
            {
                read = await stream.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break; // 1초간 더 들어오는 데이터 없음 → 유실분 확정
            }
            if (read == 0) break;
            total += read;
        }

        total.ShouldBe(expected);
    }
}
