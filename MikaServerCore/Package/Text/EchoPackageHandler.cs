using MikaServerCore.Interface;
using MikaServerCore.Network;

namespace MikaServerCore.Package.Text;

public sealed class EchoPackageHandler : IPackageHandler<TextPackageInfo>
{
    public Task HandleAsync(Session session, TextPackageInfo package)
        => session.SendLineAsync($"echo: {package.Text}");
}