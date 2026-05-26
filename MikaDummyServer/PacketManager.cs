using MikaServerCore.Network;

namespace MikaDummyServer;

public class PacketManager
{
    private readonly Dictionary<ushort, Action<MikaSession, ReadOnlyMemory<byte>>> _handlers = new();
    
    PacketManager()
    {
        Register();
    }

    void Register()
    {
        _handlers.Add((ushort)PacketId.C_EchoRequest, PacketHandler.Handle_C_EchoRequest);
    }

    public void OnRecvPacket(MikaSession session, ReadOnlyMemory<byte> packet)
    {
        if (packet.Length < 4) return;
        
        ushort packetId = BitConverter.ToUInt16(packet.Span.Slice(0, sizeof(ushort)));

        if (_handlers.TryGetValue(packetId, out var handler))
        {
            handler.Invoke(session, packet);
        }
    }
}