using System.Net;
using System.Net.Sockets;
using MikaServerCore.Network;
using Shouldly;

namespace MikaServerCore.test;

public class MikaSessionLifecycleTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>Disconnect가 여러 경로에서 불려도 Connected·Disconnected 이벤트가 각각 정확히 1번만 발화하는지 확인한다.</summary>
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

    /// <summary>서버 쪽에서 세션을 끊었을 때 클라이언트가 이를 감지해 Disconnected를 발화하고 IsConnected가 false가 되는지 확인한다.</summary>
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

    /// <summary>Disconnect 후 SendLoop까지 종료되어 StartAsync 태스크가 누수 없이 완료(회수)되는지 확인한다.</summary>
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

    /// <summary>상대가 읽지 않아 송신 큐가 포화되면, 데이터를 조용히 버리지 않고 세션을 끊는지 확인한다.</summary>
    [Fact]
    public async Task Send_Under_Backpressure_Disconnects_Session()
    {
        // peer가 읽지 않으면 OS 송신 버퍼가 차고 SendLoop이 막혀 SendQueue(1024)가 포화된다.
        // 이때 TryWrite가 실패하므로 데이터를 버리는 대신 세션을 끊는 것이 (A) 정책이다.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var connector = new MikaConnector();
        var session = await connector.ConnectAsync(IPAddress.Loopback, port);

        int disconnectedCount = 0;
        session.Disconnected += _ => Interlocked.Increment(ref disconnectedCount);
        _ = session.StartAsync();

        using var peer = await listener.AcceptTcpClientAsync().WaitAsync(Timeout);
        // peer는 일부러 읽지 않는다

        var payload = new byte[4096];
        // 포화가 일어날 때까지 계속 밀어넣는다 → 끊기면 IsConnected가 false가 된다
        var disconnected = await TestHelpers.WaitUntilAsync(() =>
        {
            for (int i = 0; i < 1000; i++)
                session.Send(payload);
            return !session.IsConnected;
        }, Timeout);

        disconnected.ShouldBeTrue();
        session.IsConnected.ShouldBeFalse();
        disconnectedCount.ShouldBeGreaterThanOrEqualTo(1);
    }
}
