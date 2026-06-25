using System.Net;
using MikaNetwork.Core.Interfaces;

namespace MikaDummyServer.Test;

/// <summary>
/// 소켓 없이 핸들러/매니저를 호출하기 위한 ISession 테스트 더블.
/// SessionId를 주입할 수 있고, Send()로 들어온 프레임 바이트를 캡처해 응답 검증에 사용한다.
/// </summary>
internal sealed class FakeSession : ISession
{
    private readonly List<byte[]> _sent = new();

    public long SessionId { get; init; }
    public EndPoint? RemoteEndPoint => null;
    public bool IsConnected { get; private set; } = true;

    /// <summary>이 세션으로 송신된 프레임([id][size][body])들의 캡처본.</summary>
    public IReadOnlyList<byte[]> Sent => _sent;

#pragma warning disable CS0067 // 테스트 더블이라 일부 이벤트는 발화하지 않음
    public event Func<ISession, ReadOnlyMemory<byte>, ValueTask>? Received;
    public event Action<ISession>? Connected;
#pragma warning restore CS0067
    public event Action<ISession>? Disconnected;

    public void Send(ReadOnlyMemory<byte> data) => _sent.Add(data.ToArray());

    public void Disconnect()
    {
        IsConnected = false;
        Disconnected?.Invoke(this);
    }

    public void Dispose() { }
}
