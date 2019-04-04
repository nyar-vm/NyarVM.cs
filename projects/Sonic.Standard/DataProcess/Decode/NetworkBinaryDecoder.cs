using Std.DataProcess.Encode;
using Std.Text.Utf8;

namespace Std.DataProcess.Decode;

/// <summary>
///     网络二进制解码器（大端序），用于大多数网络协�?///
/// </summary>
public sealed class NetworkBinaryDecoder : IDecoder
{
    private const byte _null_marker = 0xFF;

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
        if (buffer.Length < 4) return Decoded<int>.insufficient;

        var value = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        return new Decoded<int>(value, 4);
    }

    /// <inheritdoc />
    public Decoded<long> decode_i64(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 8) return Decoded<long>.insufficient;

        var hi = (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        var lo = (uint)((buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7]);
        var value = (long)((ulong)hi << 32) | lo;
        return new Decoded<long>(value, 8);
    }

    /// <inheritdoc />
    public Decoded<ulong> decode_u64(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 8) return Decoded<ulong>.insufficient;

        var hi = (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        var lo = (uint)((buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7]);
        var value = ((ulong)hi << 32) | lo;
        return new Decoded<ulong>(value, 8);
    }

    /// <inheritdoc />
    public Decoded<float> decode_f32(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 4) return Decoded<float>.insufficient;

        var bits = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
        var value = BitConverter.Int32BitsToSingle(bits);
        return new Decoded<float>(value, 4);
    }

    /// <inheritdoc />
    public Decoded<double> decode_f64(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 8) return Decoded<double>.insufficient;

        var hi = (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        var lo = (uint)((buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7]);
        var value = BitConverter.Int64BitsToDouble((long)((ulong)hi << 32) | lo);
        return new Decoded<double>(value, 8);
    }

    /// <inheritdoc />
    public Decoded<Utf8Text> decode_utf8(ReadOnlySpan<byte> buffer)
    {
        var lengthResult = decode_i32(buffer);

        if (!lengthResult.is_success) return Decoded<Utf8Text>.insufficient;

        if (lengthResult.value < 0) throw new EncodeException($"字符串长度为负数: {lengthResult.value}");

        var remaining = buffer[lengthResult.bytes_consumed..];

        if (remaining.Length < lengthResult.value) return Decoded<Utf8Text>.insufficient;

        var bytes = remaining[..lengthResult.value].ToArray();
        var text = Utf8Text.from_bytes_unchecked(bytes);
        return new Decoded<Utf8Text>(text, lengthResult.bytes_consumed + lengthResult.value);
    }

    /// <inheritdoc />
    public DecodedRawBytes decode_bytes(ReadOnlySpan<byte> buffer, int length)
    {
        if (buffer.Length < length) return DecodedRawBytes.insufficient;

        return new DecodedRawBytes(buffer[..length], length);
    }
}