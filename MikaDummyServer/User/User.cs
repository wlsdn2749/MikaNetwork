using MikaNetwork.Core.Interfaces;

namespace MikaDummyServer.User;

/// <summary>
/// Session 접속 후 로그인하면 생성되는 게임 로직 단위 객체.
/// 참조 방향은 User -> Session 단방향이며, Session은 User를 알지 못한다.
/// 생성은 반드시 <see cref="UserManager.CreateUser"/>를 통해서만 이루어진다.
/// </summary>
public sealed class User
{
    public long SessionId { get; }
    public ISession Session { get; }
    public string Id { get; }            // 로그인 시 받은 Id
    public DateTime LoggedInAt { get; }

    internal User(ISession session, string id)
    {
        SessionId  = session.SessionId;
        Session    = session;
        Id         = id;
        LoggedInAt = DateTime.UtcNow;
    }
}
