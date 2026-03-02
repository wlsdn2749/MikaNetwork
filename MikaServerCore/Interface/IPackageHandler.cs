using MikaServerCore.Network;

namespace MikaServerCore.Interface;

public interface IPackageHandler<TPackage> where TPackage : IPackageInfo
{
    Task HandleAsync(Session session, TPackage package);
}