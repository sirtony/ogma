using System.Buffers.Binary;

namespace Ogma;

internal static class IntegerExtensions
{
    public static byte[] ToLittleEndianBytes( this ushort value )
    {
        var buffer = new byte[sizeof( ushort )];
        BinaryPrimitives.WriteUInt16LittleEndian( buffer, value );
        return buffer;
    }
}
