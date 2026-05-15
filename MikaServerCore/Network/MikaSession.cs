using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace MikaServerCore.Network;

// public interface ISessionFactory
// {
//     public MikaSession Create(Socket socket);
// }
public static class MikaSessionFactory
{
    private static long _sequenceKey;
    
    public static MikaSession Create(Socket socket)
    {
        return new MikaSession(socket, CreateSessionId());
    }
    
    private static long CreateSessionId()
    {
        long newKey = Interlocked.Increment(ref _sequenceKey);
        if (0 == newKey) // overflow시 한번 더 증가시켜서 반환
        {
            newKey = Interlocked.Increment(ref _sequenceKey);
        }
        return newKey;
    }
}

public sealed class SessionManager
{
    public ConcurrentDictionary<long, MikaSession> Sessions {get; init;} = new();
    
    public int Count => Sessions.Count;
    
    public bool TryAdd(long sessionId, MikaSession session)
    {
        return Sessions.TryAdd(sessionId, session);
    }

    public bool TryRemove(long sessionId, out MikaSession? removedSession)
    {
        return Sessions.TryRemove(sessionId, out removedSession);
    }

    public bool TryGet(long sessionId, out MikaSession? session)
    {
        return Sessions.TryGetValue(sessionId, out session);
    }
}

public sealed class MikaSession : IDisposable
{
    /// <summary>
    /// _sequenceKey와 CreateSessionId()를 통해 Unique한 SessionId를 발급
    /// </summary>
    private readonly Socket _socket;
    private readonly Channel<byte[]> _sendQueue;
    private readonly Channel<byte[]> _receiveQueue;
    private readonly CancellationTokenSource _cts;

    private const int SendQueueCapacity = 1024;
    //private const int ReceiveQueueCapacity = 1024;
    
    public EndPoint? RemoteEndPoint { get; }
    public long SessionId { get; init; }
    public bool IsConnected { get; private set; } = false;
    
    public event Func<MikaSession, ReadOnlyMemory<byte>, ValueTask>? Received;
    public event Action<MikaSession>? Disconnected;
    public MikaSession(Socket socket, long sessionId)
    {
        _socket = socket;
        _sendQueue = Channel.CreateBounded<byte[]>(
            new BoundedChannelOptions(SendQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait
            });

        _receiveQueue = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        
        //
        
        SessionId = sessionId;
    }

    public void Disconnect()
    {
        if (!IsConnected)
            return;
        
        IsConnected = false;

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        
        Disconnected?.Invoke(this);
        Dispose();
    }

    public async Task StartAsync()
    {
        if (IsConnected)
            return;
        
        IsConnected = true;

        try
        {
            await ReceiveLoop();
        }
        finally
        {
            Disconnect();
        }
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
        
        Disconnect();
    }

    public void Dispose()
    {
        _socket.Dispose();
    }
}