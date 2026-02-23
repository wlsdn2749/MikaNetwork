using System.Buffers;

namespace MikaServerCore.Pipeline;

public interface IPipelineFilter<TPackageInfo> where TPackageInfo : class, IPackageInfo
{
    bool TryDecode(ref ReadOnlySequence<byte> buffer, out TPackageInfo? package);
}