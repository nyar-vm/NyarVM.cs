using Std.DataProcess.Encode;
using Std.Text.Utf8;

namespace Std.DataProcess.Decode;

/// <summary>
///     LEB128 变长整数解码器，用于 Protobuf、MQTT 剩余长度�?///
/// </summary>
public sealed class Leb128Decoder : IDecoder
{
    private const byte _null_marker = 0xF6;

    /// <inheritdoc />
    public Decoded<bool> try_read_null(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 1) return Decoded<bool>.insufficient;

        return new Decoded<bool>(buffer[0] == _null_marker, 1);
    }

    /// <inheritdoc />
    public Decoded<bool> decode_bool(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 1) return Decoded<bool>.insufficient;

        return new Decoded<bool>(buffer[0] != 0, 1);
    }

    /// <inheritdoc />
    public Decoded<int> decode_i32(ReadOnlySpan<byte> buffer)
    {
        var result = read_var_int64(buffer);

        if (!result.is_success) return Decoded<int>.insufficient;

        return new Decoded<int>((int)result.value, result.bytes_consumed);
    }

    /// <inheritdoc />
    public Decoded<long> decode_i64(ReadOnlySpan<byte> buffer)
    {
        var result = read_var_int64(buffer);

        if (!result.is_success) return Decoded<long>.insufficient;

        return new Decoded<long>((long)result.value, result.bytes_consumed);
    }

    /// <inheritdoc />
    public Decoded<ulong> decode_u64(ReadOnlySpan<byte> buffer)
    {
        return read_var_int64(buffer);
    }

    /// <inheritdoc />
    public Decoded<float> decode_f32(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 4) return Decoded<float>.insufficient;

        var bits = buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        return new Decoded<float>(BitConverter.Int32BitsToSingle(bits), 4);
    }

    /// <inheritdoc />
    public Decoded<double> decode_f64(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 8) return Decoded<double>.insufficient;

        var lo = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        var hi = (uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
        return new Decoded<double>(BitConverter.Int64BitsToDouble((long)((ulong)hi << 32) | lo), 8);
    }

    /// <inheritdoc />
    public Decoded<Utf8Text> decode_utf8(ReadOnlySpan<byte> buffer)
    {
        var lengthResult = read_var_int64(buffer);

        if (!lengthResult.is_success) return Decoded<Utf8Text>.insufficient;

        var remaining = buffer[lengthResult.bytes_consumed..];

        if (remaining.Length < (int)lengthResult.value) return Decoded<Utf8Text>.insufficient;

        var bytes = remaining[..(int)lengthResult.value].ToArray();
        return new Decoded<Utf8Text>(
            Utf8Text.from_bytes_unchecked(bytes),
            lengthResult.bytes_consumed + (int)lengthResult.value);
    }

    /// <inheritdoc />
    public DecodedRawBytes decode_bytes(ReadOnlySpan<byte> buffer, int length)
    {
        if (buffer.Length < length) return DecodedRawBytes.insufficient;

        return new DecodedRawBytes(buffer[..length], length);
    }

    /// <summary>
    ///     读取无符�?64 �?LEB128 变长整数
    /// </summary>
    private static Decoded<ulong> read_var_int64(ReadOnlySpan<byte> buffer)
    {
        ulong result = 0;
        var shift = 0;
        var index = 0;

        while (index < buffer.Length)
        {
            var b = buffer[index++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0) return new Decoded<ulong>(result, index);

            shift += 7;

            if (shift >= 70) throw new EncodeException("LEB128 编码超过 10 个字节，数据格式非法");
        }

        return Decoded<ulong>.insufficient;
    }
}