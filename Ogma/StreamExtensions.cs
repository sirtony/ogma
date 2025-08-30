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

    public static async ValueTask<ushort> ReadUInt16LittleEndianAsync(
        this Stream       stream,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        var buffer = stream.ReadExactlyAsync( sizeof( ushort ), cancellationToken );
        return BinaryPrimitives.ReadUInt16LittleEndian( await buffer );
    }

    public static ValueTask WriteUInt16LittleEndianAsync(
        this Stream       stream,
        ushort            value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull( stream, nameof( stream ) );
        var buffer = new byte[sizeof( ushort )];
        BinaryPrimitives.WriteUInt16LittleEndian( buffer, value );
        return stream.WriteAsync( buffer, cancellationToken );
    }
}
