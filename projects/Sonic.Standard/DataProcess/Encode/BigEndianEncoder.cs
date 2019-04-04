using System.Buffers.Binary;
using Std.DataProcess.Write;
using Std.Text.Utf16;
using Std.Text.Utf8;

namespace Std.DataProcess.Encode;

/// <summary>
///     大端序编解码工具，提供基本类型到大端字节序的编码与解码�?/// 直接操作 <see cref="Span{T}" /> / <see cref="ReadOnlySpan{T}" />，不依赖
///     <see cref="IBufferWriter{T}" />�?/// �?<see cref="NetworkEncoder" />�?see cref="NetworkBinaryDecoder"/> 等使用�?///
/// </summary>
internal class BigEndianEncoder : IEncoder
{
    public void encode_null(IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = 0xFF;
        writer.advance(1);
    }

    public void encode_bool(bool value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = value ? (byte)1 : (byte)0;
        writer.advance(1);
    }

    public void encode_i8(sbyte value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = (byte)value;
        writer.advance(1);
    }

    public void encode_i16(short value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(2);
        BinaryPrimitives.WriteInt16BigEndian(span, value);
        writer.advance(2);
    }

    public void encode_i32(int value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(4);
        BinaryPrimitives.WriteInt32BigEndian(span, value);
        writer.advance(4);
    }

    public void encode_i64(long value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(8);
        BinaryPrimitives.WriteInt64BigEndian(span, value);
        writer.advance(8);
    }

    public void encode_i128(Int128 value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(16);
        BinaryPrimitives.WriteInt128BigEndian(span, value);
        writer.advance(16);
    }

    public void encode_u8(byte value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = value;
        writer.advance(1);
    }

    public void encode_u16(ushort value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(2);
        BinaryPrimitives.WriteUInt16BigEndian(span, value);
        writer.advance(2);
    }

    public void encode_u32(uint value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(4);
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        writer.advance(4);
    }

    public void encode_u64(ulong value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(8);
        BinaryPrimitives.WriteUInt64BigEndian(span, value);
        writer.advance(8);
    }

    public void encode_u128(UInt128 value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(16);
        BinaryPrimitives.WriteUInt128BigEndian(span, value);
        writer.advance(16);
    }

    public void encode_f32(float value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(4);
        BinaryPrimitives.WriteSingleBigEndian(span, value);
        writer.advance(4);
    }

    public void encode_f64(double value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(8);
        BinaryPrimitives.WriteDoubleBigEndian(span, value);
        writer.advance(8);
    }

    public void encode_utf8(Utf8Text text, IBufferWriter<byte> writer)
    {
        var bytes = text.as_span();
        var span = writer.get_span(4 + bytes.Length);
        BinaryPrimitives.WriteInt32BigEndian(span, bytes.Length);
        bytes.CopyTo(span[4..]);
        writer.advance(4 + bytes.Length);
    }

    public void encode_utf16(Utf16Text text, IBufferWriter<byte> writer)
    {
        var source = text.as_span();
        var byteCount = source.Length * 2;
        var span = writer.get_span(byteCount);
        for (var i = 0; i < source.Length; i++) BinaryPrimitives.WriteUInt16BigEndian(span[(i * 2)..], source[i]);
        writer.advance(byteCount);
    }

    public void encode_bytes(ReadOnlySpan<byte> bytes, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(bytes.Length);
        bytes.CopyTo(span);
        writer.advance(bytes.Length);
    }

    /// <summary>
    ///     从跨度中大端序解�?16 位有符号整数�?    ///
    /// </summary>
    public static short decode_i16(ReadOnlySpan<byte> source)
    {
        return (short)((source[0] << 8) | source[1]);
    }

    /// <summary>
    ///     从跨度中大端序解�?16 位无符号整数�?    ///
    /// </summary>
    public static ushort decode_u16(ReadOnlySpan<byte> source)
    {
        return (ushort)((source[0] << 8) | source[1]);
    }

    /// <summary>
    ///     从跨度中大端序解�?32 位有符号整数�?    ///
    /// </summary>
    public static int decode_i32(ReadOnlySpan<byte> source)
    {
        return (source[0] << 24) | (source[1] << 16) | (source[2] << 8) | source[3];
    }

    /// <summary>
    ///     从跨度中大端序解�?32 位无符号整数�?    ///
    /// </summary>
    public static uint decode_u32(ReadOnlySpan<byte> source)
    {
        return (uint)((source[0] << 24) | (source[1] << 16) | (source[2] << 8) | source[3]);
    }

    /// <summary>
    ///     从跨度中大端序解�?64 位有符号整数�?    ///
    /// </summary>
    public static long decode_i64(ReadOnlySpan<byte> source)
    {
        return ((long)source[0] << 56) | ((long)source[1] << 48) |
               ((long)source[2] << 40) | ((long)source[3] << 32) |
               ((long)source[4] << 24) | ((long)source[5] << 16) |
               ((long)source[6] << 8) | source[7];
    }

    /// <summary>
    ///     从跨度中大端序解�?64 位无符号整数�?    ///
    /// </summary>
    public static ulong decode_u64(ReadOnlySpan<byte> source)
    {
        return (ulong)decode_i64(source);
    }

    /// <summary>
    ///     从跨度中大端序解�?32 位单精度浮点数�?    ///
    /// </summary>
    public static float decode_f32(ReadOnlySpan<byte> source)
    {
        var bits = decode_i32(source);
        return BitConverter.Int32BitsToSingle(bits);
    }

    /// <summary>
    ///     从跨度中大端序解�?64 位双精度浮点数�?    ///
    /// </summary>
    public static double decode_f64(ReadOnlySpan<byte> source)
    {
        var bits = decode_i64(source);
        return BitConverter.Int64BitsToDouble(bits);
    }

    /// <summary>
    ///     以大端序�?16 位有符号整数写入跨度�?    ///
    /// </summary>
    public static void encode_i16(Span<byte> destination, short value)
    {
        destination[0] = (byte)(value >> 8);
        destination[1] = (byte)value;
    }

    /// <summary>
    ///     以大端序�?16 位无符号整数写入跨度�?    ///
    /// </summary>
    public static void encode_u16(Span<byte> destination, ushort value)
    {
        destination[0] = (byte)(value >> 8);
        destination[1] = (byte)value;
    }

    /// <summary>
    ///     以大端序�?32 位有符号整数写入跨度�?    ///
    /// </summary>
    public static void encode_i32(Span<byte> destination, int value)
    {
        destination[0] = (byte)(value >> 24);
        destination[1] = (byte)(value >> 16);
        destination[2] = (byte)(value >> 8);
        destination[3] = (byte)value;
    }

    /// <summary>
    ///     以大端序�?32 位无符号整数写入跨度�?    ///
    /// </summary>
    public static void encode_u32(Span<byte> destination, uint value)
    {
        encode_i32(destination, (int)value);
    }

    /// <summary>
    ///     以大端序�?64 位有符号整数写入跨度�?    ///
    /// </summary>
    public static void encode_i64(Span<byte> destination, long value)
    {
        destination[0] = (byte)(value >> 56);
        destination[1] = (byte)(value >> 48);
        destination[2] = (byte)(value >> 40);
        destination[3] = (byte)(value >> 32);
        destination[4] = (byte)(value >> 24);
        destination[5] = (byte)(value >> 16);
        destination[6] = (byte)(value >> 8);
        destination[7] = (byte)value;
    }

    /// <summary>
    ///     以大端序�?64 位无符号整数写入跨度�?    ///
    /// </summary>
    public static void encode_u64(Span<byte> destination, ulong value)
    {
        encode_i64(destination, (long)value);
    }

    /// <summary>
    ///     以大端序�?32 位单精度浮点数写入跨度�?    ///
    /// </summary>
    public static void encode_f32(Span<byte> destination, float value)
    {
        encode_i32(destination, BitConverter.SingleToInt32Bits(value));
    }

    /// <summary>
    ///     以大端序�?64 位双精度浮点数写入跨度�?    ///
    /// </summary>
    public static void encode_f64(Span<byte> destination, double value)
    {
        encode_i64(destination, BitConverter.DoubleToInt64Bits(value));
    }
}