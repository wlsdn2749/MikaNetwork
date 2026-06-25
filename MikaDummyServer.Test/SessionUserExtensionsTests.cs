using MikaDummyServer.User;
using Shouldly;

namespace MikaDummyServer.Test;

/// <summary>
/// session.GetUser() 헬퍼 검증: SessionId → UserManager 조회로 User를 꺼내는 경로.
/// </summary>
public class SessionUserExtensionsTests
{
    [Fact]
    public void GetUser_After_Login_Returns_User()
    {
        const long sid = 1002001;
        var session = new FakeSession { SessionId = sid };
        try
        {
            var created = UserManager.Instance.CreateUser(session, "hero");

            session.GetUser().ShouldBeSameAs(created);
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }

    [Fact]
    public void GetUser_Before_Login_Returns_Null()
    {
        var session = new FakeSession { SessionId = 1002002 };

        session.GetUser().ShouldBeNull();
    }
}
