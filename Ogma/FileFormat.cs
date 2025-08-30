using System.IO.Compression;
using System.Text.Json;

namespace Ogma;

internal static class FileFormat
{
    private static readonly byte[] MagicId = "OGMA"u8.ToArray();
    private const           ushort Version = 2;

    public static async ValueTask WriteAsync<TKey, TValue>(
        Document<TKey, TValue> document,
        StoreOptions           options,
        CancellationToken      cancellationToken = default
    ) where TKey : notnull
    {
        var tmpPath = Path.ChangeExtension( options.Path, "ogma.tmp" );
        await using( var file = File.Open( tmpPath, FileMode.Create, FileAccess.Write, FileShare.None ) )
        {
            await file.WriteAsync( FileFormat.MagicId, cancellationToken );
            await file.WriteUInt16LittleEndianAsync( FileFormat.Version, cancellationToken );
            await using var compressionStream = new BrotliStream( file, options.CompressionLevel );
            await JsonSerializer.SerializeAsync( compressionStream, document, cancellationToken: cancellationToken );
            await compressionStream.FlushAsync( cancellationToken );
            await file.FlushAsync( cancellationToken );
        }

        File.Move( tmpPath, options.Path, true );
    }

    public static async ValueTask<Document<TKey, TValue>> ReadAsync<TKey, TValue>(
        StoreOptions      options,
        CancellationToken cancellationToken = default
    )
    {
        await using var file = File.Open( options.Path, FileMode.Open, FileAccess.Read, FileShare.Read );

        var magicBuf = await file.ReadExactlyAsync( FileFormat.MagicId.Length, cancellationToken );
        if( !FileFormat.MagicId.SequenceEqual( magicBuf ) )
            throw new InvalidDataException( "file is not a valid Ogma store" );

        var version = await file.ReadUInt16LittleEndianAsync( cancellationToken );
        if( FileFormat.Version != version )
        {
            throw new InvalidDataException(
                $"file has format version {version}, but this library only supports version {FileFormat.Version}"
            );
        }

        await using var decompressionStream = new BrotliStream( file, CompressionMode.Decompress );
        var document = await JsonSerializer.DeserializeAsync<Document<TKey, TValue>>(
            decompressionStream,
            cancellationToken: cancellationToken
        );

        return document ?? throw new InvalidDataException( "file is corrupted" );
    }
}
