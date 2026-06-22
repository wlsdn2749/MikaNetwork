#if UNITY_5_3_OR_NEWER

public class NetworkManager : SingletonMonobehavior<NetworkManager>
{
    private readonly MikaClient _client = new();
    private readonly ServerPacketManager _packetManager = new();

    void Awake()
    {
    }

    public async void Start()
    {  
        _client.PacketReceived += (session, data) =>
        {
            _packetManager.OnRecvPacket(session, data, job =>
            {
                NetworkMessageQueue.Instance.Push(job);
            });
            
            return default;
        }

        await _client.ConnectAsync("127.0.0.1", 10010);
    }

    void Update()
    {
        NetworkMessageQueue.Instance.Flush(); 
    }

    public void Send<T>(T packet) where T : IPacket
    {
        _client.Send(packet);
    }
}

#endif