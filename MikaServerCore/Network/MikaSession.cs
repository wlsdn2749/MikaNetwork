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
    private readonly Socket                     _socket;
    private readonly Channel<byte[]>            _sendQueue;
    private readonly MikaRecvBuffer             _recvBuffer;
    private readonly CancellationTokenSource    _cts;

    private const int SendQueueCapacity = 1024;
    private const int MaxRecvBufferSize = 64 * 1024; 
    
    public EndPoint? RemoteEndPoint => _socket.RemoteEndPoint;
    public long SessionId { get; init; }
    public bool IsConnected { get; private set; } = false;
    
    public event Func<MikaSession, ReadOnlyMemory<byte>, ValueTask>? Received;
    public event Action<MikaSession>? Disconnected;
    public event Action<MikaSession>? Connected;
    
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
        _recvBuffer = new MikaRecvBuffer(MaxRecvBufferSize);
        _cts = new CancellationTokenSource();
        SessionId = sessionId;
    }

    public void Disconnect()
    {
        if (!IsConnected)
            return;

        IsConnected = false;

        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
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
        Connected?.Invoke(this);
        
        try
        {
            await Task.WhenAll(ReceiveLoop(), SendLoop());
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
        while (IsConnected)
        {
            // 여기서 recvBuffer에 직접쓸 공간 가져오기 새 할당 XX
            var buffer =_recvBuffer.GetWritableMemory(MikaPacketBuilder.MaxPacketSize);
            int received = await _socket.ReceiveAsync(buffer);
            
            if (received == 0)
            {
                break; 
            }
            
            _recvBuffer.AdvanceWrite(received);
            while (_recvBuffer.ReadableBytes >= MikaPacketBuilder.HeaderSize) // PacketHeader
            {
                var size = MikaPacketBuilder.ReadSize(_recvBuffer.GetReadableSpan());
                
                // 읽은 사이즈가 Header보다 작거나, 패킷 사이즈보다 클 경우 차단
                if (size < MikaPacketBuilder.HeaderSize || size > MikaPacketBuilder.MaxPacketSize)
                {
                    Disconnect();
                    return;
                }
                
                if (size <= _recvBuffer.ReadableBytes)
                {
                    var data = MikaPacketBuilder.ReadPacket(_recvBuffer.GetReadableSpan());
                    _recvBuffer.AdvanceRead(size);
                    await OnReceived(data);
                }
                else
                {
                    break;
                }
            }
        }
        
        Disconnect();
    }

    public void Send(byte[] data)
    {
        _sendQueue.Writer.TryWrite(data); 
    }

    private async Task SendLoop()
    {
        try
        {
            while (await _sendQueue.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_sendQueue.Reader.TryRead(out byte[]? data))
                {
                    if (!IsConnected) return;

                    await _socket.SendAsync(data, SocketFlags.None);
                }
            }
        }
        catch (Exception)
        {
            Disconnect();
        }
    }

    public void Dispose()
    {
        _socket.Dispose();
        _cts.Cancel();
    }
    
    
}