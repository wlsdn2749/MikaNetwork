using System.Net;

namespace MikaServerCore;

public sealed class MikaServerOptions
{
    public required MikaAcceptorOptions AcceptorOpt { get; init; }
    public MikaSessionOptions SessionOpt { get; init; } = new();
}

public sealed class MikaAcceptorOptions
{
    public required int Port { get; init; }
    public IPAddress LocalAddr { get; init; } = IPAddress.Loopback;
    public int Backlog { get; init; } = 65535;
}

public sealed class MikaSessionOptions
{
    public int SendQueueCapacity { get; init; } = 1024;
    public int ReceiveBufferSize { get; init; } = 8192;
}