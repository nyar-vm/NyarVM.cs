using System.Text;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Spine.Encode;

/// <summary>
///     Spine 二进制编码器，将 C# 数据结构编码的Spine 二进制格式的
/// </summary>
/// <remarks>
///     Spine 二进制格式使用小端序存储数值，字符串使用长度前缀（LEB128 编码的int）加 UTF-8 字节的方式存储的
///     编码器支持骨骼、插槽、附件、动画等 Spine 核心数据结构的
/// </remarks>
public ref struct SpineEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始的<see cref="SpineEncoder" /> 结构的新实例的
    /// </summary>
    /// <param name="buffer">要写入的目标字节缓冲区的/param>
    public SpineEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position => _writer.position;

    /// <summary>
    ///     写入文件头魔的"skeleton" 的ASCII 字节的
    /// </summary>
    public void write_header()
    {
        var magic = Encoding.ASCII.GetBytes("skeleton");
        _writer.write(magic);
    }

    /// <summary>
    ///     写入哈希值字符串的
    /// </summary>
    /// <param name="hash">哈希字符串的/param>
    public void write_hash(string? hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            _writer.write_u8(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(hash);
        _writer.write_u8((byte)bytes.Length);
        _writer.write(bytes);
    }

    /// <summary>
    ///     写入版本号字符串的
    /// </summary>
    /// <param name="version">版本号字符串的/param>
    public void write_version(string? version)
    {
        if (string.IsNullOrEmpty(version))
        {
            _writer.write_u8(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(version);
        _writer.write_u8((byte)bytes.Length);
        _writer.write(bytes);
    }

    /// <summary>
    ///     写入 Spine 变长字符串的
    /// </summary>
    /// <param name="value">要写入的字符串，null 或空字符串会写入长度 0的/param>
    public void write_string(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _writer.write_leb128_i32(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        _writer.write_leb128_i32(bytes.Length);
        _writer.write(bytes);
    }

    /// <summary>
    ///     写入 Spine 布尔值的
    /// </summary>
    /// <param name="value">布尔值的/param>
    public void write_boolean(bool value)
    {
        _writer.write_u8(value ? (byte)1 : (byte)0);
    }

    /// <summary>
    ///     写入单精度浮点数（小端序）的
    /// </summary>
    /// <param name="value">浮点数值的/param>
    public void write_float(float value)
    {
        _writer.write_f32_le(value);
    }

    /// <summary>
    ///     写入颜色值（4 字节 RGBA）的
    /// </summary>
    /// <param name="r">
    ///     红色分量的/param>
    ///     <param name="g">
    ///         绿色分量的/param>
    ///         <param name="b">
    ///             蓝色分量的/param>
    ///             <param name="a">透明度分量的/param>
    public void write_color(byte r, byte g, byte b, byte a)
    {
        _writer.write_u8(r);
        _writer.write_u8(g);
        _writer.write_u8(b);
        _writer.write_u8(a);
    }

    /// <summary>
    ///     写入无符的8 位整数的
    /// </summary>
    /// <param name="value">要写入的值的/param>
    public void write_u_int8(byte value)
    {
        _writer.write_u8(value);
    }

    /// <summary>
    ///     写入无符的16 位整数（小端序）的
    /// </summary>
    /// <param name="value">要写入的值的/param>
    public void write_u_int16(ushort value)
    {
        _writer.write_u16_le(value);
    }

    /// <summary>
    ///     写入无符的32 位整数（小端序）的
    /// </summary>
    /// <param name="value">要写入的值的/param>
    public void write_u_int32(uint value)
    {
        _writer.write_u32_le(value);
    }

    /// <summary>
    ///     写入有符的32 位整数（小端序）的
    /// </summary>
    /// <param name="value">要写入的值的/param>
    public void write_int32(int value)
    {
        _writer.write_i32_le(value);
    }

    /// <summary>
    ///     写入 LEB128 编码的有符号 32 位整数的
    /// </summary>
    /// <param name="value">要写入的值的/param>
    public void write_leb128_int32(int value)
    {
        _writer.write_leb128_i32(value);
    }
}