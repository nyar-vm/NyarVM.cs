using System.Buffers.Binary;
using System.Text;

namespace Std.DL.Flux;

/// <summary>
///     轻量级 protobuf 写入器 —— 直接实现 protobuf 线格式，用于生成 ONNX 模型文件
///     不依赖 Google.Protobuf、Grpc.Tools 等外部 NuGet 包
/// </summary>
public sealed class OnnxProtoWriter
{
    private readonly MemoryStream _stream;

    /// <summary>
    ///     创建 protobuf 写入器
    /// </summary>
    public OnnxProtoWriter()
    {
        _stream = new MemoryStream();
    }

    #region Protobuf 线格式常量

    /// <summary>
    ///     Varint 线类型（field type 0）：用于 int32, int64, uint32, uint64, sint32, sint64, bool, enum
    /// </summary>
    public const int WireTypeVarint = 0;

    /// <summary>
    ///     64-bit 线类型（field type 1）：用于 fixed64, sfixed64, double
    /// </summary>
    public const int WireType64Bit = 1;

    /// <summary>
    ///     长度分隔线类型（field type 2）：用于 string, bytes, embedded messages, repeated fields
    /// </summary>
    public const int WireTypeLengthDelimited = 2;

    /// <summary>
    ///     32-bit 线类型（field type 5）：用于 fixed32, sfixed32, float
    /// </summary>
    public const int WireType32Bit = 5;

    #endregion

    #region 基础写入方法

    /// <summary>
    ///     写入 Varint 编码的无符号整数
    /// </summary>
    /// <param name="value">无符号整数值</param>
    public void WriteVarint(ulong value)
    {
        Span<byte> buffer = stackalloc byte[10];
        var offset = 0;

        while (value > 0x7F)
        {
            buffer[offset++] = (byte)((value & 0x7F) | 0x80);
            value >>= 7;
        }

        buffer[offset++] = (byte)value;
        _stream.Write(buffer[..offset]);
    }

    /// <summary>
    ///     写入字段头（field_number << 3 | wire_type）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="wireType">线类型</param>
    public void WriteFieldHeader(int fieldNumber, int wireType)
    {
        WriteVarint((ulong)((fieldNumber << 3) | wireType));
    }

    /// <summary>
    ///     写入有符号 int32 字段（使用 ZigZag 编码）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">int32 值</param>
    public void WriteSInt32(int fieldNumber, int value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        WriteVarint(ZigZagEncode32(value));
    }

    /// <summary>
    ///     写入 int32 字段（protobuf 兼容：负数按 10 字节 varint 编码）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">int32 值</param>
    public void WriteInt32(int fieldNumber, int value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        if (value < 0)
            WriteVarint((ulong)value);
        else
            WriteVarint((uint)value);
    }

    /// <summary>
    ///     写入 int64 字段
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">int64 值</param>
    public void WriteInt64(int fieldNumber, long value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        WriteVarint((ulong)value);
    }

    /// <summary>
    ///     写入 uint32 字段
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">uint32 值</param>
    public void WriteUInt32(int fieldNumber, uint value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        WriteVarint(value);
    }

    /// <summary>
    ///     写入 uint64 字段
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">uint64 值</param>
    public void WriteUInt64(int fieldNumber, ulong value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        WriteVarint(value);
    }

    /// <summary>
    ///     写入 float 字段（32-bit 固定长度）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">float 值</param>
    public void WriteFloat(int fieldNumber, float value)
    {
        WriteFieldHeader(fieldNumber, WireType32Bit);
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// <summary>
    ///     写入 double 字段（64-bit 固定长度）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">double 值</param>
    public void WriteDouble(int fieldNumber, double value)
    {
        WriteFieldHeader(fieldNumber, WireType64Bit);
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// <summary>
    ///     写入 string 字段（长度前缀 + UTF-8 编码）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">字符串值</param>
    public void WriteString(int fieldNumber, string value)
    {
        WriteFieldHeader(fieldNumber, WireTypeLengthDelimited);
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarint((ulong)bytes.Length);
        _stream.Write(bytes);
    }

    /// <summary>
    ///     写入 bytes 字段（长度前缀 + 原始字节）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">字节数组</param>
    public void WriteBytes(int fieldNumber, byte[] value)
    {
        WriteFieldHeader(fieldNumber, WireTypeLengthDelimited);
        WriteVarint((ulong)value.Length);
        _stream.Write(value);
    }

    /// <summary>
    ///     写入 bool 字段（varint 0 或 1）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">bool 值</param>
    public void WriteBool(int fieldNumber, bool value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        WriteVarint(value ? 1UL : 0UL);
    }

    /// <summary>
    ///     写入枚举字段（varint 编码的 int32）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="value">枚举整数值</param>
    public void WriteEnum(int fieldNumber, int value)
    {
        WriteFieldHeader(fieldNumber, WireTypeVarint);
        WriteVarint((ulong)value);
    }

    /// <summary>
    ///     写入嵌套消息字段（先写入子消息到临时缓冲区，再写入长度前缀 + 子消息字节）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="writeAction">子消息写入委托</param>
    public void WriteMessage(int fieldNumber, Action<OnnxProtoWriter> writeAction)
    {
        var child = new OnnxProtoWriter();
        writeAction(child);
        var childBytes = child.ToArray();

        WriteFieldHeader(fieldNumber, WireTypeLengthDelimited);
        WriteVarint((ulong)childBytes.Length);
        _stream.Write(childBytes);
    }

    /// <summary>
    ///     写入 repeated int64 字段（每个元素独立写入为 varint）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="values">int64 值集合</param>
    public void WriteRepeatedInt64(int fieldNumber, IEnumerable<long> values)
    {
        foreach (var v in values)
        {
            WriteFieldHeader(fieldNumber, WireTypeVarint);
            WriteVarint((ulong)v);
        }
    }

    /// <summary>
    ///     写入 repeated int32 字段（每个元素独立写入为 varint）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="values">int32 值集合</param>
    public void WriteRepeatedInt32(int fieldNumber, IEnumerable<int> values)
    {
        foreach (var v in values)
        {
            WriteFieldHeader(fieldNumber, WireTypeVarint);
            WriteVarint((ulong)v);
        }
    }

    /// <summary>
    ///     写入 repeated float 字段（打包方式：长度前缀 + 连续 float 字节）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="values">float 值集合</param>
    public void WriteRepeatedFloat(int fieldNumber, IEnumerable<float> values)
    {
        var floatList = values as IList<float> ?? [.. values];
        var byteCount = floatList.Count * 4;

        WriteFieldHeader(fieldNumber, WireTypeLengthDelimited);
        WriteVarint((ulong)byteCount);

        Span<byte> buffer = stackalloc byte[4];
        foreach (var f in floatList)
        {
            BinaryPrimitives.WriteSingleLittleEndian(buffer, f);
            _stream.Write(buffer);
        }
    }

    /// <summary>
    ///     写入 repeated string 字段（每个元素独立写入为 length-delimited）
    /// </summary>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="values">字符串集合</param>
    public void WriteRepeatedString(int fieldNumber, IEnumerable<string> values)
    {
        foreach (var s in values) WriteString(fieldNumber, s);
    }

    /// <summary>
    ///     写入 repeated 嵌套消息字段
    /// </summary>
    /// <typeparam name="T">消息元素类型</typeparam>
    /// <param name="fieldNumber">字段编号</param>
    /// <param name="items">消息元素集合</param>
    /// <param name="writeAction">每个元素的写入委托</param>
    public void WriteRepeatedMessage<T>(int fieldNumber, IEnumerable<T> items, Action<OnnxProtoWriter, T> writeAction)
    {
        foreach (var item in items) WriteMessage(fieldNumber, w => writeAction(w, item));
    }

    /// <summary>
    ///     返回已写入的所有字节
    /// </summary>
    /// <returns>protobuf 编码的字节数组</returns>
    public byte[] ToArray()
    {
        return _stream.ToArray();
    }

    #endregion

    #region ZigZag 编码

    /// <summary>
    ///     ZigZag 编码 32 位有符号整数
    ///     将有符号整数映射为无符号整数：0→0, -1→1, 1→2, -2→3, ...
    /// </summary>
    /// <param name="value">有符号整数</param>
    /// <returns>ZigZag 编码后的无符号整数</returns>
    private static ulong ZigZagEncode32(int value)
    {
        return (ulong)((value << 1) ^ (value >> 31));
    }

    /// <summary>
    ///     ZigZag 编码 64 位有符号整数
    /// </summary>
    /// <param name="value">有符号长整数</param>
    /// <returns>ZigZag 编码后的无符号长整数</returns>
    private static ulong ZigZagEncode64(long value)
    {
        return (ulong)((value << 1) ^ (value >> 63));
    }

    #endregion
}