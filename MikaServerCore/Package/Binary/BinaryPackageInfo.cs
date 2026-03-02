using MikaServerCore.Interface;

namespace MikaServerCore.Package.Binary;

public record BinaryPackageInfo(ushort PacketId, ushort PacketSize, ReadOnlyMemory<byte> Body) : IPackageInfo;
