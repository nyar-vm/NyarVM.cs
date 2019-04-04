using Std.DataProcess.Encode;
using Std.DataProcess.Write;

namespace Std.DataProcess.Enframe;

/// <summary>
///     长度前缀封帧器，将载荷加上长度前缀后写入输出器�?/// 支持 1/2/4 字节的固定长度前缀�?LEB128 变长前缀�?///
/// </summary>
public sealed class LengthPrefixedEnframer : IEnframer
{
    private readonly int _prefix_size;
    private readonly bool _use_var_int;
    private readonly Leb128Encoder _var_int_encoder = new();

    /// <summary>
    ///     使用固定长度前缀初始化封帧器�?    ///
    /// </summary>
    /// <param name="prefixSize">长度前缀的字节数�?�? �?4）�?/param>
    public LengthPrefixedEnframer(int prefixSize = 4)
    {
        if (prefixSize is not (1 or 2 or 4))
            throw new ArgumentOutOfRangeException(nameof(prefixSize), "长度前缀字节数必须为 1�? �?4");

        _prefix_size = prefixSize;
        _use_var_int = false;
    }

    /// <summary>
    ///     使用 LEB128 变长前缀初始化封帧器�?    ///
    /// </summary>
    /// <param name="useVarInt">必须�?<c>true</c>，表示使�?LEB128 变长前缀�?/param>
    public LengthPrefixedEnframer(bool useVarInt)
    {
        _prefix_size = 0;
        _use_var_int = useVarInt;
    }

    /// <inheritdoc />
    public void enframe(ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        if (_use_var_int)
            _var_int_encoder.encode_u64((ulong)payload.Length, writer);
        else
            write_fixed_prefix(payload.Length, writer);

        var span = writer.get_span(payload.Length);
        payload.CopyTo(span);
        writer.advance(payload.Length);
    }

    /// <summary>
    ///     写入固定长度前缀�?    ///
    /// </summary>
    private void write_fixed_prefix(int length, IBufferWriter<byte> writer)
    {
        switch (_prefix_size)
        {
            case 1:
            {
                if (length > byte.MaxValue) throw new FramingException($"载荷长度 {length} 超过 1 字节前缀最大�?{byte.MaxValue}");

                var span = writer.get_span(1);
                span[0] = (byte)length;
                writer.advance(1);
                break;
            }
            case 2:
            {
                if (length > ushort.MaxValue)
                    throw new FramingException($"载荷长度 {length} 超过 2 字节前缀最大�?{ushort.MaxValue}");

                var span = writer.get_span(2);
                span[0] = (byte)(length >> 8);
                span[1] = (byte)length;
                writer.advance(2);
                break;
            }
            case 4:
            {
                var span = writer.get_span(4);
                span[0] = (byte)(length >> 24);
                span[1] = (byte)(length >> 16);
                span[2] = (byte)(length >> 8);
                span[3] = (byte)length;
                writer.advance(4);
                break;
            }
        }
    }
}