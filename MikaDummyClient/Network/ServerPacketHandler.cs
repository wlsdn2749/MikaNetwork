using System;
using MikaNetwork.Core.Interfaces;
using MikaProtocol;

namespace MikaDummyClient
{
    public static class ServerPacketHandler
    {
        [PacketHandler]
        public static void Handle_S_EchoResponse(ISession session, S_EchoResponse res)
        {
            Console.WriteLine($"[Client] Recv Echo: {res.Message}");
        }

        [PacketHandler]
        public static void Handle_S_PongRequest(ISession session, S_PongRequest req)
        {
            
        }
        
        [PacketHandler]
        public static void Handle_S_LoginResponse(ISession session, S_LoginResponse req)
        {
            
        }
    }
}

