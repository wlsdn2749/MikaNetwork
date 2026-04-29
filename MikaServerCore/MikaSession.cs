using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace MikaServerCore;

public sealed class MikaSession
{
    /// <summary>
    /// _sequenceKey와 CreateSessionId()를 통해 Unique한 SessionId를 발급
    /// </summary>
    private static long _sequenceKey = 0;
    private long CreateSessionId()
    {
        long newKey = Interlocked.Increment(ref _sequenceKey);
        if (0 == newKey)
        {
            newKey = Interlocked.Increment(ref _sequenceKey);
        }
        return newKey;
    }
    
    private readonly Socket _socket;
    private readonly Channel<byte[]> _sendQueue;
    private readonly CancellationTokenSource _cts;
    
    public EndPoint? RemoteEntPoint { get; }
    public long SessionId { get; init; }
    public bool IsConnected { get; }

    public event Func<MikaSession, ReadOnlyMemory<byte>, ValueTask>? Received;
    public event Func<MikaSession, ValueTask>? Disconnected;
    public MikaSession(Socket socket)
    {
        _socket = socket;
        SessionId = CreateSessionId();
    }

    public async Task Connected()
    {
        try
        {
            await ReceiveLoop();
        }
        catch (Exception Ex)
        {
            await Connected();
        }
    }

    public async Task DisConnect()
    {
        
    }

    public async Task Accepted()
    {
        await ReceiveLoop();
    }
    
    public async Task OnReceived(ReadOnlyMemory<byte> data)
    {
        var handler = Received;

        if (handler is null) return;
        
        await handler(this, data);
    }
    

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[4096];

        while (IsConnected)
        {
            int received = await _socket.ReceiveAsync(buffer);

            if (received == 0)
            {
                break; 
            }

            ReadOnlyMemory<byte> data = buffer.AsMemory(0, received);

            await OnReceived(data);
        }
        
        await Disconnect();
    }
}