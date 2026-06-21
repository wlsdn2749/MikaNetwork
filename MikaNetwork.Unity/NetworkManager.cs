#if UNITY_5_3_OR_NEWER

public class NetworkManager : SingletonMonobehavior<NetworkManager>
{
    private readonly MikaClient _client = new();
    private readonly ServerPacketManager _packetManager = new();

    void Awake()
    {
    }

    void Update()
    {
    }

    public void Send<T>(T packet) where T : IPacket
    {
        _client.Send(packet);
    }
}

#endif