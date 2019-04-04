using Std.DataProcess.Write;
using Std.Text.Utf16;
using Std.Text.Utf8;

namespace Std.DataProcess.Encode;

/// <summary>
///     网络二进制编码器（大端序），用于大多数网络协议�?/// 无状态，每次调用独立。大端字节序编码委托�?<see cref="BigEndianEncoder" />�?///
/// </summary>
public sealed class NetworkEncoder : IEncoder
{
    private readonly BigEndianEncoder _be = new();

    public void encode_null(IBufferWriter<byte> writer)
    {
        _be.encode_null(writer);
    }

    public void encode_bool(bool value, IBufferWriter<byte> writer)
    {
        _be.encode_bool(value, writer);
    }

    public void encode_i8(sbyte value, IBufferWriter<byte> writer)
    {
        _be.encode_i8(value, writer);
    }

    public void encode_i16(short value, IBufferWriter<byte> writer)
    {
        _be.encode_i16(value, writer);
    }

    public void encode_i32(int value, IBufferWriter<byte> writer)
    {
        _be.encode_i32(value, writer);
    }

    public void encode_i64(long value, IBufferWriter<byte> writer)
    {
        _be.encode_i64(value, writer);
    }

    public void encode_i128(Int128 value, IBufferWriter<byte> writer)
    {
        _be.encode_i128(value, writer);
    }

    public void encode_u8(byte value, IBufferWriter<byte> writer)
    {
        _be.encode_u8(value, writer);
    }

    public void encode_u16(ushort value, IBufferWriter<byte> writer)
    {
        _be.encode_u16(value, writer);
    }

    public void encode_u32(uint value, IBufferWriter<byte> writer)
    {
        _be.encode_u32(value, writer);
    }

    public void encode_u64(ulong value, IBufferWriter<byte> writer)
    {
        _be.encode_u64(value, writer);
    }

    public void encode_u128(UInt128 value, IBufferWriter<byte> writer)
    {
        _be.encode_u128(value, writer);
    }

    public void encode_f32(float value, IBufferWriter<byte> writer)
    {
        _be.encode_f32(value, writer);
    }

    public void encode_f64(double value, IBufferWriter<byte> writer)
    {
        _be.encode_f64(value, writer);
    }

    public void encode_utf8(Utf8Text text, IBufferWriter<byte> writer)
    {
        _be.encode_utf8(text, writer);
    }

    public void encode_utf16(Utf16Text text, IBufferWriter<byte> writer)
    {
        _be.encode_utf16(text, writer);
    }

    public void encode_bytes(ReadOnlySpan<byte> bytes, IBufferWriter<byte> writer)
    {
        _be.encode_bytes(bytes, writer);
    }
}