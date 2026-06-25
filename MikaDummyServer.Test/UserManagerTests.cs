using MikaDummyServer.User;
using Shouldly;

namespace MikaDummyServer.Test;

/// <summary>
/// UserManager(게임 로직 레이어의 User 저장소) 단위 검증.
/// 전역 싱글턴이므로 테스트마다 고유 SessionId를 쓰고, 끝나면 등록분을 정리한다.
/// </summary>
public class UserManagerTests
{
    [Fact]
    public void CreateUser_Stores_And_Retrieves_Same_Instance()
    {
        const long sid = 1001001;
        var session = new FakeSession { SessionId = sid };
        try
        {
            var user = UserManager.Instance.CreateUser(session, "hero");

            user.ShouldNotBeNull();
            UserManager.Instance.TryGet(sid, out var found).ShouldBeTrue();
            found.ShouldBeSameAs(user);
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }

    [Fact]
    public void CreateUser_DuplicateSessionId_Returns_Null_And_Keeps_Original()
    {
        const long sid = 1001002;
        var session = new FakeSession { SessionId = sid };
        try
        {
            var first = UserManager.Instance.CreateUser(session, "hero");
            var second = UserManager.Instance.CreateUser(session, "other");

            first.ShouldNotBeNull();
            second.ShouldBeNull();

            // 기존 등록이 덮어써지지 않아야 한다
            UserManager.Instance.TryGet(sid, out var found).ShouldBeTrue();
            found.ShouldBeSameAs(first);
            found!.Id.ShouldBe("hero");
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }

    [Fact]
    public void CreateUser_Populates_Fields()
    {
        const long sid = 1001003;
        var session = new FakeSession { SessionId = sid };
        try
        {
            var before = DateTime.UtcNow;
            var user = UserManager.Instance.CreateUser(session, "hero");
            var after = DateTime.UtcNow;

            user.ShouldNotBeNull();
            user!.SessionId.ShouldBe(sid);
            user.Session.ShouldBeSameAs(session);
            user.Id.ShouldBe("hero");
            user.LoggedInAt.ShouldBeInRange(before, after);
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }

    [Fact]
    public void TryGet_Unknown_Session_Returns_False()
    {
        UserManager.Instance.TryGet(1001999, out var user).ShouldBeFalse();
        user.ShouldBeNull();
    }

    [Fact]
    public void TryRemove_Removes_Registered_User()
    {
        const long sid = 1001004;
        var session = new FakeSession { SessionId = sid };
        var created = UserManager.Instance.CreateUser(session, "hero");

        UserManager.Instance.TryRemove(sid, out var removed).ShouldBeTrue();
        removed.ShouldBeSameAs(created);
        UserManager.Instance.TryGet(sid, out _).ShouldBeFalse();
    }
}
