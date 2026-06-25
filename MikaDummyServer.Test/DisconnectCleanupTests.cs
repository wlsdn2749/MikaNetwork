using MikaDummyServer.User;
using Shouldly;

namespace MikaDummyServer.Test;

/// <summary>
/// 접속 해제 시 User 정리 와이어링 검증.
/// "MikaServer가 Disconnected를 발화한다"는 MikaNetwork.Tests에 이미 있으므로 소켓 재현은 생략하고,
/// NetworkManager가 거는 정리 람다(Disconnected → UserManager.TryRemove)의 계약만 검증한다.
/// </summary>
public class DisconnectCleanupTests
{
    [Fact]
    public void Disconnect_Removes_User()
    {
        const long sid = 1004001;
        var session = new FakeSession { SessionId = sid };

        // NetworkManager.Initialize()가 거는 것과 동일한 와이어링
        session.Disconnected += s => UserManager.Instance.TryRemove(s.SessionId, out _);

        try
        {
            UserManager.Instance.CreateUser(session, "hero").ShouldNotBeNull();
            UserManager.Instance.TryGet(sid, out _).ShouldBeTrue();

            session.Disconnect();

            UserManager.Instance.TryGet(sid, out _).ShouldBeFalse();
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }
}
