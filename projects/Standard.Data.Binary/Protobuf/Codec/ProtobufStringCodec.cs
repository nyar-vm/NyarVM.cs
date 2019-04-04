using System.Text;
using Std.Codec;

namespace Std.Data.Binary.Protobuf.Codec;

/// <summary>
///     Protobuf 字符串编解码器，基于 <see cref="Leb128UInt32" /> 提供 Protobuf 长度前缀字符串编码的
/// </summary>
public readonly struct ProtobufStringCodec : ICodec<string>
{
    private readonly Leb128UInt32 _varint_codec;

    /// <summary>
    ///     初始�?see cref="ProtobufStringCodec" /> 结构的新实例�?
    /// </summary>
    public ProtobufStringCodec()
    {
        _varint_codec = new Leb128UInt32();
    }

    /// <inheritdoc />
    public int get_size(string value)
    {
        return -1;
    }

    /// <inheritdoc />
    public void encode(string value, Span<byte> destination)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        _varint_codec.encode((uint)bytes.Length, destination);
        bytes.CopyTo(destination[leb128_encoded_size((uint)bytes.Length)..]);
    }

    /// <inheritdoc />
    public string decode(ReadOnlySpan<byte> source)
    {
        var length = (int)_varint_codec.decode(source);
        var leb128Size = leb128_encoded_size((uint)length);
        return Encoding.UTF8.GetString(source.Slice(leb128Size, length));
    }

    private static int leb128_encoded_size(uint value)
    {
        var size = 0;
        do
        {
            value >>= 7;
            size++;
        } while (value != 0);

        return size;
    }
}