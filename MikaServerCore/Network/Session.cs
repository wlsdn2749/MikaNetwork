using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MikaServerCore.Pipeline;

namespace MikaServerCore.Network;

public class Session
{
    private Socket  _socket = null!;
    private readonly IPipelineFilter<TextPackageInfo> _filter = new LinePipelineFilter();
    
    public Action<Session, TextPackageInfo>? OnPackageReceived;
    
    public void Init(Socket socket)
    {
        _socket = socket;
    }
    public async Task<bool> ConnectAsync(IPEndPoint ipEndPoint)
    {
        try
        {
            _socket = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(ipEndPoint);
            _ = StartAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connect Failed: {ex.Message}");
            return false;
        }
    }
    
    public async Task StartAsync()
    {
        // WhileLoop를 돌면서, ReceiveAsync 호출

        try
        {
            using var stream = new NetworkStream(_socket, ownsSocket: false);
            var reader = PipeReader.Create(stream);

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                while (_filter.TryDecode(ref buffer, out var package))
                {
                    if (package is not null)
                        OnPackage(package);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }

            await reader.CompleteAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Session error: {ex.Message}");
        }
        finally
        {
            OnDisconnected();
            Disconnect();
        }
    }

    public async Task SendAsync(byte[] buffer)
    {
        _ = await _socket.SendAsync(buffer, SocketFlags.None);
    }

    public Task SendLineAsync(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line + "\r\n");
        return SendAsync(bytes);
    }

    protected virtual void OnPackage(TextPackageInfo package)
    {
        Console.WriteLine($"Received: {package.Text}");
        OnPackageReceived?.Invoke(this, package);
    }
    public virtual void OnReceive(byte[] buffer)
    {
        string message = Encoding.UTF8.GetString(buffer);
        Console.WriteLine($"Received {message}");
    }
    public virtual void OnDisconnected() {}

    public void Disconnect()
    {
        _socket?.Close();
    }
}