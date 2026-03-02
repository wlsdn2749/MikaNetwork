using System.Buffers;

namespace MikaServerCore.Interface;

public interface IPipelineFilter<TPackageInfo> where TPackageInfo : class, IPackageInfo
{
    bool TryDecode(ref ReadOnlySequence<byte> buffer, out TPackageInfo? package);
}