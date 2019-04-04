using System.Buffers.Binary;
using System.Text;
using Olympus.Athena.Core;

namespace Olympus.Athena.Serialization;

#region BinaryDecoder 二进制读取器

/// <summary>
///     基于 <see cref="ReadOnlySpan{T}" /> 的零拷贝二进制读取器，以小端字节序从缓冲区读取数据
/// </summary>
public ref struct BinaryDecoder
{
    #region 字段

    private readonly ReadOnlySpan<byte> _buffer;

    #endregion

    #region 构造函数

    /// <summary>
    ///     使用指定的缓冲区创建二进制读取器
    /// </summary>
    /// <param name="buffer">源数据缓冲区</param>
    public BinaryDecoder(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        Position = 0;
    }

    #endregion

    #region 属性

    /// <summary>
    ///     当前读取位置
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     缓冲区剩余可读取的字节数
    /// </summary>
    public int Remaining => _buffer.Length - Position;

    #endregion

    #region 固定长度读取

    /// <summary>
    ///     以小端字节序读取 <see cref="long" /> 值（8 字节）
    /// </summary>
    /// <returns>读取的值</returns>
    public long ReadInt64()
    {
        const int size = 8;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.Slice(Position, size));
        Position += size;
        return value;
    }

    /// <summary>
    ///     以小端字节序读取 <see cref="int" /> 值（4 字节）
    /// </summary>
    /// <returns>读取的值</returns>
    public int ReadInt32()
    {
        const int size = 4;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(Position, size));
        Position += size;
        return value;
    }

    /// <summary>
    ///     以小端字节序读取 <see cref="double" /> 值（8 字节）
    /// </summary>
    /// <returns>读取的值</returns>
    public double ReadFloat64()
    {
        const int size = 8;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var bits = BinaryPrimitives.ReadInt64LittleEndian(_buffer.Slice(Position, size));
        Position += size;
        return BitConverter.Int64BitsToDouble(bits);
    }

    /// <summary>
    ///     读取 <see cref="bool" /> 值（1 字节，0 为 <c>false</c>，非 0 为 <c>true</c>）
    /// </summary>
    /// <returns>读取的值</returns>
    public bool ReadBool()
    {
        const int size = 1;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var value = _buffer[Position] != 0;
        Position += size;
        return value;
    }

    /// <summary>
    ///     读取 <see cref="Guid" /> 值（16 字节）
    /// </summary>
    /// <returns>读取的值</returns>
    public Guid ReadGuid()
    {
        const int size = 16;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var value = new Guid(_buffer.Slice(Position, size));
        Position += size;
        return value;
    }

    /// <summary>
    ///     读取 <see cref="Core.UUIDv7" /> 值（16 字节），从 <see cref="Guid" /> 构造
    /// </summary>
    /// <returns>读取的值</returns>
    public UUIDv7 ReadUUIDv7()
    {
        var guid = ReadGuid();
        return UUIDv7.FromGuid(guid);
    }

    #endregion

    #region 变长读取

    /// <summary>
    ///     读取长度前缀的字节数组（先读 4 字节长度，再读对应长度的数据）
    /// </summary>
    /// <returns>读取的字节数组</returns>
    public byte[] ReadBytes()
    {
        var length = ReadInt32();
        if (length < 0) throw new InvalidOperationException("字节数组长度为负数");

        if (Position + length > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var data = _buffer.Slice(Position, length).ToArray();
        Position += length;
        return data;
    }

    /// <summary>
    ///     读取长度前缀的 UTF-8 编码字符串（先读 4 字节字节长度，再读 UTF-8 数据并解码）
    /// </summary>
    /// <returns>读取的字符串</returns>
    public string ReadString()
    {
        var byteLength = ReadInt32();
        if (byteLength < 0) throw new InvalidOperationException("字符串字节长度为负数");

        if (Position + byteLength > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var value = Encoding.UTF8.GetString(_buffer.Slice(Position, byteLength));
        Position += byteLength;
        return value;
    }

    /// <summary>
    ///     读取 <see cref="Core.DataValue" />，先读 1 字节类型标签，再根据类型读取对应数据
    /// </summary>
    /// <returns>读取的值</returns>
    public DataValue ReadDataValue()
    {
        const int tagSize = 1;
        if (Position + tagSize > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        var tag = (DataType)_buffer[Position];
        Position += tagSize;

        return tag switch
        {
            DataType.Int64 => ReadInt64(),
            DataType.Float64 => ReadFloat64(),
            DataType.String => (DataValue)ReadString(),
            DataType.Bytes => (DataValue)ReadBytes(),
            DataType.Guid => ReadGuid(),
            DataType.Bool => ReadBool(),
            DataType.Null => (DataValue)(string?)null,
            _ => throw new InvalidOperationException($"未知的 DataType: {tag}")
        };
    }

    #endregion
}

#endregion