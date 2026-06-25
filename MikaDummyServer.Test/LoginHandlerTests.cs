using MemoryPack;
using MikaDummyServer.Network;
using MikaDummyServer.User;
using MikaNetwork.Core.Network;
using MikaProtocol;
using Shouldly;

namespace MikaDummyServer.Test;

/// <summary>
/// Handle_C_LoginRequest를 실제 디스패치 경로로 검증한다.
/// new ClientPacketManager()가 소스젠 RegisterAll을 호출하므로 실제 등록된 핸들러가 실행된다.
/// </summary>
public class LoginHandlerTests
{
    private static byte[] FrameLogin(string id)
        => MikaPacketBuilder.MakePacket(
            (ushort)PacketId.C_LoginRequest,
            MemoryPackSerializer.Serialize(new C_LoginRequest { Id = id }));

    private static S_LoginResponse ParseLoginResponse(byte[] framed)
    {
        // 프레임: [id:2][size:2][body]
        BitConverter.ToUInt16(framed, 0).ShouldBe((ushort)PacketId.S_LoginResponse);
        return MemoryPackSerializer.Deserialize<S_LoginResponse>(framed.AsSpan(4))!;
    }

    [Fact]
    public void Login_Creates_User_With_RequestId()
    {
        const long sid = 1003001;
        var pm = new ClientPacketManager();
        var session = new FakeSession { SessionId = sid };
        try
        {
            pm.OnRecvPacket(session, FrameLogin("hero"));

            UserManager.Instance.TryGet(sid, out var user).ShouldBeTrue();
            user!.Id.ShouldBe("hero");
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }

    [Fact]
    public void Login_Responds_With_Success()
    {
        const long sid = 1003002;
        var pm = new ClientPacketManager();
        var session = new FakeSession { SessionId = sid };
        try
        {
            pm.OnRecvPacket(session, FrameLogin("hero"));

            session.Sent.Count.ShouldBe(1);
            var resp = ParseLoginResponse(session.Sent[0]);
            resp.Success.ShouldBeTrue();
            resp.SessionId.ShouldBe(sid);
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }

    [Fact]
    public void Login_Twice_Same_Session_Second_Responds_Failure()
    {
        const long sid = 1003003;
        var pm = new ClientPacketManager();
        var session = new FakeSession { SessionId = sid };
        try
        {
            pm.OnRecvPacket(session, FrameLogin("hero"));
            pm.OnRecvPacket(session, FrameLogin("hero"));

            session.Sent.Count.ShouldBe(2);
            ParseLoginResponse(session.Sent[0]).Success.ShouldBeTrue();
            ParseLoginResponse(session.Sent[1]).Success.ShouldBeFalse();
        }
        finally { UserManager.Instance.TryRemove(sid, out _); }
    }
}
