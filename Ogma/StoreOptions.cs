using System.IO.Compression;

namespace Ogma;

public readonly record struct StoreOptions()
{
    public string           Path             { get; init; } = "./ogma.kvs";
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;
}
