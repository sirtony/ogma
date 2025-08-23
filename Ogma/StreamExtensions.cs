using System.Buffers.Binary;

namespace Ogma;

internal static class StreamExtensions
{
    public static async ValueTask<byte[]> ReadExactlyAsync(
        this Stream       stream,
        int               count,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        if( count < 0 ) throw new ArgumentOutOfRangeException( nameof( count ), "Count must be non-negative." );

        var buffer = new byte[count];
        await stream.ReadExactlyAsync( buffer, cancellationToken );

        return buffer;
    }

    public static async ValueTask<byte> ReadUInt8Async(
        this Stream       stream,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        var b = stream.ReadExactlyAsync( 1, cancellationToken );
        return ( await b )[0];
    }

    public static async ValueTask<uint> ReadUInt32LittleEndianAsync(
        this Stream       stream,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        var buffer = stream.ReadExactlyAsync( sizeof( uint ), cancellationToken );
        return BinaryPrimitives.ReadUInt32LittleEndian( await buffer );
    }

    public static ValueTask WriteUInt32LittleEndianAsync(
        this Stream       stream,
        uint              value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        var buffer = new byte[sizeof( uint )];
        BinaryPrimitives.WriteUInt32LittleEndian( buffer, value );
        return stream.WriteAsync( buffer, cancellationToken );
    }

    public static async ValueTask<ushort> ReadUInt16LittleEndianAsync(
        this Stream       stream,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        var buffer = stream.ReadExactlyAsync( sizeof( ushort ), cancellationToken );
        return BinaryPrimitives.ReadUInt16LittleEndian( await buffer );
    }
}
