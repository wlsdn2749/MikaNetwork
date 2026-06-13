using MikaProtocol;
using MikaServerCore.Network;

namespace MikaDummyServer;

public static class ClientPacketHandler
{
    [PacketHandler]
    public static void Handle_C_EchoRequest(MikaSession session, C_EchoRequest req)
    {
        Console.WriteLine($"[Server] Recv Echo: {req.Message}");

        // 응답도 객체로 송신 (직렬화/프레이밍은 SendPacket이 처리)
        session.SendPacket(new S_EchoResponse { Message = req.Message });
    }
}
