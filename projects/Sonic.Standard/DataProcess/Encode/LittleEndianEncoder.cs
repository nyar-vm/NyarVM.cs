using Std.DataProcess.Write;
using Std.Text.Utf16;
using Std.Text.Utf8;

namespace Std.DataProcess.Encode;

/// <summary>
///     小端序编码器，用于本地优化场景�?/// 无状态，每次调用独立�?///
/// </summary>
public sealed class LittleEndianEncoder : IEncoder
{
    /// <inheritdoc />
    public void encode_null(IBufferWriter<byte> writer)
    {
        var span = writer.get_span(1);
        span[0] = 0xFF;
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
        var span = writer.get_span(4);
        BitConverter.TryWriteBytes(span, value);
        writer.advance(4);
    }

    /// <inheritdoc />
    public void encode_i64(long value, IBufferWriter<byte> writer)
    {
        var span = writer.get_span(8);
        BitConverter.TryWriteBytes(span, value);
        writer.advance(8);
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
        var span = writer.get_span(8);
        BitConverter.TryWriteBytes(span, value);
        writer.advance(8);
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
        encode_i32(utf8Bytes.Length, writer);
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
}