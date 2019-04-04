using System.Buffers.Binary;
using System.Text;
using Olympus.Athena.Core;

namespace Olympus.Athena.Serialization;

#region BinaryEncoder 二进制写入器

/// <summary>
///     基于 <see cref="Span{T}" /> 的零拷贝二进制写入器，以小端字节序将数据写入缓冲区
/// </summary>
public ref struct BinaryEncoder
{
    #region 字段

    private readonly Span<byte> _buffer;

    #endregion

    #region 构造函数

    /// <summary>
    ///     使用指定的缓冲区创建二进制写入器
    /// </summary>
    /// <param name="buffer">目标写入缓冲区</param>
    public BinaryEncoder(Span<byte> buffer)
    {
        _buffer = buffer;
        Position = 0;
    }

    #endregion

    #region 属性

    /// <summary>
    ///     当前写入位置
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    ///     已写入的字节数
    /// </summary>
    public int BytesWritten => Position;

    /// <summary>
    ///     缓冲区剩余可写入的字节数
    /// </summary>
    public int Remaining => _buffer.Length - Position;

    #endregion

    #region 固定长度写入

    /// <summary>
    ///     以小端字节序写入 <see cref="long" /> 值（8 字节）
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteInt64(long value)
    {
        const int size = 8;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(Position, size), value);
        Position += size;
    }

    /// <summary>
    ///     以小端字节序写入 <see cref="int" /> 值（4 字节）
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteInt32(int value)
    {
        const int size = 4;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(Position, size), value);
        Position += size;
    }

    /// <summary>
    ///     以小端字节序写入 <see cref="double" /> 值（8 字节）
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteFloat64(double value)
    {
        const int size = 8;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(Position, size), BitConverter.DoubleToInt64Bits(value));
        Position += size;
    }

    /// <summary>
    ///     写入 <see cref="bool" /> 值（1 字节，0 表示 <c>false</c>，1 表示 <c>true</c>）
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteBool(bool value)
    {
        const int size = 1;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        _buffer[Position] = value ? (byte)1 : (byte)0;
        Position += size;
    }

    /// <summary>
    ///     写入 <see cref="Guid" /> 值（16 字节）
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteGuid(Guid value)
    {
        const int size = 16;
        if (Position + size > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        value.TryWriteBytes(_buffer.Slice(Position, size));
        Position += size;
    }

    /// <summary>
    ///     写入 <see cref="Core.UUIDv7" /> 值，先转换为 <see cref="Guid" /> 后写入（16 字节）
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteUUIDv7(UUIDv7 value)
    {
        WriteGuid(value.ToGuid());
    }

    #endregion

    #region 变长写入

    /// <summary>
    ///     写入长度前缀的字节数组（4 字节长度 + 数据）
    /// </summary>
    /// <param name="data">要写入的字节数据</param>
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        WriteInt32(data.Length);
        WriteFixedBytes(data);
    }

    /// <summary>
    ///     以 UTF-8 编码写入长度前缀的字符串（4 字节字节长度 + UTF-8 数据）
    /// </summary>
    /// <param name="value">要写入的字符串</param>
    public void WriteString(string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        if (Position + 4 + maxByteCount > _buffer.Length)
        {
            var actualCount = Encoding.UTF8.GetByteCount(value);
            if (Position + 4 + actualCount > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");
        }

        var utf8Bytes = Encoding.UTF8.GetBytes(value);
        WriteInt32(utf8Bytes.Length);
        WriteFixedBytes(utf8Bytes);
    }

    /// <summary>
    ///     写入 <see cref="Core.DataValue" />，先写入 1 字节类型标签，再写入对应类型的数据
    /// </summary>
    /// <param name="value">要写入的值</param>
    public void WriteDataValue(DataValue value)
    {
        const int tagSize = 1;
        if (Position + tagSize > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        _buffer[Position] = (byte)value.Kind;
        Position += tagSize;

        switch (value.Kind)
        {
            case DataType.Int64:
                WriteInt64(value.AsInt64());
                break;
            case DataType.Float64:
                WriteFloat64(value.AsFloat64());
                break;
            case DataType.String:
                WriteString(value.AsString());
                break;
            case DataType.Bytes:
                WriteBytes(value.AsBytes());
                break;
            case DataType.Guid:
                WriteGuid(value.AsGuid());
                break;
            case DataType.Bool:
                WriteBool(value.AsBool());
                break;
            case DataType.Null:
                break;
            default:
                throw new InvalidOperationException($"未知的 DataType: {value.Kind}");
        }
    }

    #endregion

    #region 原始写入

    /// <summary>
    ///     直接写入原始字节数据，不附加长度前缀
    /// </summary>
    /// <param name="data">要写入的字节数据</param>
    public void WriteFixedBytes(ReadOnlySpan<byte> data)
    {
        if (Position + data.Length > _buffer.Length) throw new InvalidOperationException("缓冲区空间不足");

        data.CopyTo(_buffer[Position..]);
        Position += data.Length;
    }

    #endregion
}

#endregion