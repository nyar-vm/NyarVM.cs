using Core.Stream;

namespace Std.Stream;

/// <summary>
///     位流包装器，实现 IBitStream 接口，提供位级别的读写操作
/// </summary>
public sealed class BitStreamWrapper : IBitStream
{
    /// <summary>
    ///     底层流
    /// </summary>
    private readonly System.IO.Stream _stream;

    /// <summary>
    ///     初始化位流包装器
    /// </summary>
    /// <param name="stream">底层流</param>
    public BitStreamWrapper(System.IO.Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    ///     是否可读
    /// </summary>
    public bool can_read => _stream.CanRead;

    /// <summary>
    ///     是否可写
    /// </summary>
    public bool can_write => _stream.CanWrite;

    /// <summary>
    ///     写入一个位
    /// </summary>
    /// <param name="value">位值</param>
    public void write_bit(bool value)
    {
        _stream.WriteByte(value ? (byte)1 : (byte)0);
    }

    /// <summary>
    ///     读取一个位
    /// </summary>
    /// <returns>位值</returns>
    public bool read_bit()
    {
        var b = _stream.ReadByte();
        return b == 1;
    }

    /// <summary>
    ///     写入一个字节
    /// </summary>
    /// <param name="value">字节值</param>
    public void write_byte(byte value)
    {
        _stream.WriteByte(value);
    }

    /// <summary>
    ///     读取一个字节
    /// </summary>
    /// <returns>字节值</returns>
    public byte read_byte()
    {
        return (byte)_stream.ReadByte();
    }
}