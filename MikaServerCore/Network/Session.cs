using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;

using MikaServerCore.Interface;
using MikaServerCore.Package.Text;

namespace MikaServerCore.Network;

public class Session
{
    // Socket used for all network I/O in this session.
    // It is injected via Init (server side) or created in ConnectAsync (client side).
    private Socket _socket = null!;

    // Decodes incoming byte stream into text packages.
    // Default behavior uses LinePipelineFilter, but callers can inject another filter.
    private IPipelineFilter<TextPackageInfo> _filter;

    // Event raised when a complete package is received.
    public Action<Session, TextPackageInfo>? OnPackageReceived;

    // Create a session with optional pipeline filter injection.
    // When filter is null, LinePipelineFilter is used for backward-compatible behavior.
    public Session(IPipelineFilter<TextPackageInfo>? filter = null)
    {
        _filter = filter ?? new LinePipelineFilter();
    }

    // Initialize this session with an already accepted socket.
    public void Init(Socket socket)
    {
        _socket = socket;
    }

    // Replace pipeline filter at runtime.
    // This must be called before StartAsync to affect decoding for the session.
    public void SetPipelineFilter(IPipelineFilter<TextPackageInfo> filter)
    {
        _filter = filter;
    }

    // Connect to remote endpoint as a client.
    // On success, starts the receive loop in the background.
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

    // Read data from the socket using PipeReader and decode complete packages.
    // Always runs disconnect cleanup in finally.
    public async Task StartAsync()
    {
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

    // Send raw bytes to peer.
    public async Task SendAsync(byte[] buffer)
    {
        _ = await _socket.SendAsync(buffer, SocketFlags.None);
    }

    // Send text as UTF-8 line protocol terminated by CRLF.
    public Task SendLineAsync(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line + "\r\n");
        return SendAsync(bytes);
    }

    // Internal package dispatch point.
    // Writes a log and forwards package to external handler.
    protected virtual void OnPackage(TextPackageInfo package)
    {
        Console.WriteLine($"Received: {package.Text}");
        OnPackageReceived?.Invoke(this, package);
    }

    // Extension point for raw byte receive handling.
    // Not used by current line-based protocol path.
    public virtual void OnReceive(byte[] buffer)
    {
        string message = Encoding.UTF8.GetString(buffer);
        Console.WriteLine($"Received {message}");
    }

    // Extension point invoked before socket close.
    public virtual void OnDisconnected() {}

    // Close underlying socket connection.
    public void Disconnect()
    {
        _socket?.Close();
    }
}
