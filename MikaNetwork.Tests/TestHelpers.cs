using System.Diagnostics;
using System.Net;
using MikaNetwork.Server;

namespace MikaServerCore.test;

/// <summary>
/// 루프백에 포트 0(자동 할당)으로 떠 있는 MikaServer를 감싸는 테스트 픽스처.
/// 테스트마다 독립된 서버/포트를 쓰므로 병렬 실행해도 충돌하지 않는다.
/// </summary>
public sealed class LoopbackServer : IDisposable
{
    public MikaServer Server { get; }
    public int Port { get; }

    public LoopbackServer()
    {
        Server = new MikaServer(IPAddress.Loopback, 0);
        Server.Listen();
        Port = ((IPEndPoint)Server.EndPoint).Port;
    }

    public void Dispose() => Server.Dispose();
}

public static class TestHelpers
{
    /// <summary>
    /// 조건이 참이 될 때까지 폴링. SpinWait.SpinUntil과 달리 스레드를 점유하지 않는다.
    /// </summary>
    public static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return true;
            await Task.Delay(10);
        }
        return condition();
    }
}
