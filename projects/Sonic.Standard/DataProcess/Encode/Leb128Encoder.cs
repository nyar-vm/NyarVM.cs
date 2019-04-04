using Std.DataProcess.Write;
using Std.Text.Utf16;
using Std.Text.Utf8;

namespace Std.DataProcess.Encode;

/// <summary>
///     LEB128 变长整数编码器，用于 Protobuf、MQTT 剩余长度等�?/// 无状态，每次调用独立�?///
/// </summary>
public sealed class Leb128Encoder : IEncoder
{
    /// <inheritdoc />
    public void encode_null(IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = 0xF6;
        writer.advance(1);
    }

    /// <inheritdoc />
    public void encode_bool(bool value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = value ? (byte)1 : (byte)0;
        writer.advance(1);
    }

    public void encode_i8(sbyte value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public void encode_i16(short value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void encode_i32(int value, IBufferWriter<byte> writer)
    {
        write_var_int32((uint)value, writer);
    }

    /// <inheritdoc />
    public void encode_i64(long value, IBufferWriter<byte> writer)
    {
        write_var_int64((ulong)value, writer);
    }

    public void encode_i128(Int128 value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public void encode_u8(byte value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public void encode_u16(ushort value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public void encode_u32(uint value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void encode_u64(ulong value, IBufferWriter<byte> writer)
    {
        write_var_int64(value, writer);
    }

    public void encode_u128(UInt128 value, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void encode_f32(float value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(4);
        BitConverter.TryWriteBytes(span, value);
        writer.advance(4);
    }

    /// <inheritdoc />
    public void encode_f64(double value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(8);
        BitConverter.TryWriteBytes(span, value);
        writer.advance(8);
    }

    /// <inheritdoc />
    public void encode_utf8(Utf8Text text, IBufferWriter<byte> writer)
    {
        var utf8Bytes = text.as_span();
        write_var_int64((ulong)utf8Bytes.Length, writer);
        var span = writer.get_span(utf8Bytes.Length);
        utf8Bytes.CopyTo(span);
        writer.advance(utf8Bytes.Length);
    }

    public void encode_utf16(Utf16Text text, IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void encode_bytes(ReadOnlySpan<byte> bytes, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(bytes.Length);
        bytes.CopyTo(span);
        writer.advance(bytes.Length);
    }

    /// <summary>
    ///     写入无符�?32 �?LEB128 变长整数�?    ///
    /// </summary>
    private static void write_var_int32(uint value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(5);

        var index = 0;
        while (value > 0x7F)
        {
            span[index++] = (byte)(value | 0x80);
            value >>= 7;
        }

        span[index++] = (byte)value;
        writer.advance(index);
    }

    /// <summary>
    ///     写入无符�?64 �?LEB128 变长整数�?    ///
    /// </summary>
    private static void write_var_int64(ulong value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(10);

        var index = 0;
        while (value > 0x7F)
        {
            span[index++] = (byte)(value | 0x80);
            value >>= 7;
        }

        span[index++] = (byte)value;
        writer.advance(index);
    }
}