using System.Text;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Spine.Decode;

/// <summary>
///     Spine 二进制解码器，将 Spine 二进制格式解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     Spine 二进制格式使用小端序存储数值，字符串使用长度前缀（LEB128 编码的int）加 UTF-8 字节的方式存储的
///     解码器支持骨骼、插槽、附件、动画等 Spine 核心数据结构的
/// </remarks>
public ref struct SpineDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="SpineDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要解码的 Spine 二进制数据的/param>
    public SpineDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position => _buffer.position;

    /// <summary>
    ///     获取流的总长度的
    /// </summary>
    public int length => _buffer.length;

    /// <summary>
    ///     获取一个值，该值指示是否已到达流的末尾的
    /// </summary>
    public bool is_end_of_stream => _buffer.is_end;

    /// <summary>
    ///     读取并验证文件头魔数的
    /// </summary>
    /// <returns>如果魔数匹配则返的true，否则返的false的/returns>
    public bool read_header()
    {
        var expectedMagic = Encoding.ASCII.GetBytes("skeleton");
        var actualMagic = _buffer.read_bytes(expectedMagic.Length).ToArray();
        return actualMagic.SequenceEqual(expectedMagic);
    }

    /// <summary>
    ///     读取哈希值字符串的
    /// </summary>
    /// <returns>哈希字符串，如果不存在则返回空字符串的/returns>
    public string read_hash()
    {
        var hashLength = _buffer.read_u8();

        if (hashLength == 0) return string.Empty;

        return _buffer.read_string(hashLength);
    }

    /// <summary>
    ///     读取版本号字符串的
    /// </summary>
    /// <returns>版本号字符串，如果不存在则返回空字符串的/returns>
    public string read_version()
    {
        var versionLength = _buffer.read_u8();

        if (versionLength == 0) return string.Empty;

        return _buffer.read_string(versionLength);
    }

    /// <summary>
    ///     读取 Spine 变长字符串的
    /// </summary>
    /// <returns>读取的字符串，如果长度为 0 或负数则返回 null的/returns>
    public string? read_string()
    {
        var length = _buffer.read_leb128_i32();

        if (length <= 0) return null;

        return _buffer.read_string(length);
    }

    /// <summary>
    ///     读取 Spine 布尔值的
    /// </summary>
    /// <returns>布尔值的/returns>
    public bool read_boolean()
    {
        return _buffer.read_u8() != 0;
    }

    /// <summary>
    ///     读取单精度浮点数（小端序）的
    /// </summary>
    /// <returns>浮点数值的/returns>
    public float read_float()
    {
        return _buffer.read_f32_le();
    }

    /// <summary>
    ///     读取颜色值（4 字节 RGBA）的
    /// </summary>
    /// <returns>包含 R、G、B、A 四个分量的元组的/returns>
    public (byte R, byte G, byte B, byte A) read_color()
    {
        var r = _buffer.read_u8();
        var g = _buffer.read_u8();
        var b = _buffer.read_u8();
        var a = _buffer.read_u8();
        return (r, g, b, a);
    }

    /// <summary>
    ///     读取无符的8 位整数的
    /// </summary>
    /// <returns>无符的8 位整数值的/returns>
    public byte read_u_int8()
    {
        return _buffer.read_u8();
    }

    /// <summary>
    ///     读取无符的16 位整数（小端序）的
    /// </summary>
    /// <returns>无符的16 位整数值的/returns>
    public ushort read_u_int16()
    {
        return _buffer.read_u16_le();
    }

    /// <summary>
    ///     读取无符的32 位整数（小端序）的
    /// </summary>
    /// <returns>无符的32 位整数值的/returns>
    public uint read_u_int32()
    {
        return _buffer.read_u32_le();
    }

    /// <summary>
    ///     读取有符的32 位整数（小端序）的
    /// </summary>
    /// <returns>有符的32 位整数值的/returns>
    public int read_int32()
    {
        return _buffer.read_i32_le();
    }

    /// <summary>
    ///     读取 LEB128 编码的有符号 32 位整数的
    /// </summary>
    /// <returns>解码后的有符的32 位整数值的/returns>
    public int read_leb128_int32()
    {
        return _buffer.read_leb128_i32();
    }

    /// <summary>
    ///     设置流中的位置的
    /// </summary>
    /// <param name="offset">字节偏移量的/param>
    public void seek(int offset)
    {
        _buffer.position = offset;
    }
}