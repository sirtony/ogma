using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ZstdSharp;

namespace Ogma;

internal static class FileFormat
{
    private static readonly byte[] MagicId          = "OGMA"u8.ToArray();
    private const           ushort Version          = 1;
    private const           int    SaltSize         = 16;
    private const           int    NonceSize        = 12;
    private const           int    TagSize          = 16;
    private const           int    KeySize          = 32;
    private const           int    ChecksumSize     = 64; // SHA-512
    private const           int    ArgonIterations  = 10;
    private const           int    ArgonMemory      = 20 * 1024;
    private const           int    CompressionLevel = 22;

    public static async ValueTask WriteAsync<TKey, TValue>(
        string                 path,
        Document<TKey, TValue> document,
        string?                password          = null,
        CancellationToken      cancellationToken = default
    ) where TKey : notnull
    {
        var tmpPath = Path.ChangeExtension( path, "tmp" );
        await using( var file = File.Open( tmpPath, FileMode.Create, FileAccess.Write, FileShare.None ) )
        {
            byte[] header =
            [
                ..FileFormat.MagicId,
                ..FileFormat.Version.ToLittleEndianBytes(),
                password is null ? (byte)0b0000_0000 : (byte)0b0000_0001,
            ];

            await file.WriteAsync( header, cancellationToken );
            await using var ms     = new MemoryStream();
            var             writer = new BsonBinaryWriter( ms, BsonBinaryWriterSettings.Defaults );
            BsonSerializer.Serialize( writer, document );

            await ms.FlushAsync( cancellationToken );
            ms.Position = 0;

            var checksum   = await SHA3_512.HashDataAsync( ms, cancellationToken );
            var data       = ms.ToArray();
            var compressor = new Compressor( FileFormat.CompressionLevel );
            var compressed = compressor.Wrap( data ).ToArray();

            if( password is null )
            {
                await file.WriteAsync( checksum, cancellationToken );
                await file.WriteUInt32LittleEndianAsync( (uint)data.Length, cancellationToken );
                await file.WriteAsync( compressed, cancellationToken );
            }
            else
            {
                var       salt      = RandomNumberGenerator.GetBytes( FileFormat.SaltSize );
                var       nonce     = RandomNumberGenerator.GetBytes( FileFormat.NonceSize );
                var       key       = await FileFormat.DeriveKeyAsync( password, salt, header );
                using var aes       = new AesGcm( key, FileFormat.TagSize );
                var       encrypted = new byte[compressed.Length];
                var       tag       = new byte[FileFormat.TagSize];

                aes.Encrypt( nonce, compressed, encrypted, tag, header );

                await file.WriteAsync( salt,     cancellationToken );
                await file.WriteAsync( nonce,    cancellationToken );
                await file.WriteAsync( tag,      cancellationToken );
                await file.WriteAsync( checksum, cancellationToken );
                await file.WriteUInt32LittleEndianAsync( (uint)encrypted.Length, cancellationToken );
                await file.WriteAsync( encrypted, cancellationToken );

                Array.Clear( key,   0, key.Length );
                Array.Clear( salt,  0, salt.Length );
                Array.Clear( nonce, 0, nonce.Length );
                Array.Clear( tag,   0, tag.Length );
            }

            await file.FlushAsync( cancellationToken );
        }

        File.Move( tmpPath, path, true );
    }

    public static async ValueTask<Document<TKey, TValue>> ReadAsync<TKey, TValue>(
        string            path,
        string?           password          = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var file = File.Open( path, FileMode.Open, FileAccess.Read, FileShare.Read );

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

        var    flags       = await file.ReadUInt8Async( cancellationToken );
        var    isEncrypted = ( flags & 0b0000_0001 ) != 0;
        byte[] header      = [..magicBuf, ..version.ToLittleEndianBytes(), flags];
        var    salt        = new byte[FileFormat.SaltSize];
        var    nonce       = new byte[FileFormat.NonceSize];
        var    tag         = new byte[FileFormat.TagSize];
        var    checksum    = new byte[FileFormat.ChecksumSize];

        if( isEncrypted )
        {
            await file.ReadExactlyAsync( salt,  cancellationToken );
            await file.ReadExactlyAsync( nonce, cancellationToken );
            await file.ReadExactlyAsync( tag,   cancellationToken );
        }

        await file.ReadExactlyAsync( checksum, cancellationToken );
        var len       = await file.ReadUInt32LittleEndianAsync( cancellationToken );
        var data      = await file.ReadExactlyAsync( (int)len, cancellationToken );
        var decrypted = Array.Empty<byte>();

        if( isEncrypted )
        {
            if( password is null )
                throw new InvalidDataException( "file is encrypted, but no password was provided" );

            var       key = await FileFormat.DeriveKeyAsync( password, salt, header );
            using var aes = new AesGcm( key, FileFormat.TagSize );

            Array.Resize( ref decrypted, data.Length );
            aes.Decrypt( nonce, data, tag, decrypted, header );

            Array.Clear( key,   0, key.Length );
            Array.Clear( salt,  0, salt.Length );
            Array.Clear( nonce, 0, nonce.Length );
            Array.Clear( tag,   0, tag.Length );

            data = decrypted;
        }

        var             decompressor = new Decompressor();
        var             decompressed = decompressor.Unwrap( data ).ToArray();
        await using var ms           = new MemoryStream( decompressed );

        return BsonSerializer.Deserialize<Document<TKey, TValue>>( ms );
    }

    private static async ValueTask<byte[]> DeriveKeyAsync( string password, byte[] salt, byte[] associatedData )
    {
        var passBuf = Encoding.UTF8.GetBytes( password );
        using var argon2 = new Argon2id( passBuf )
        {
            Iterations          = FileFormat.ArgonIterations,
            MemorySize          = FileFormat.ArgonMemory,
            Salt                = salt,
            DegreeOfParallelism = Environment.ProcessorCount,
            AssociatedData      = associatedData,
        };

        return await argon2.GetBytesAsync( FileFormat.KeySize );
    }
}
