using System.Runtime.InteropServices;

namespace Nyar.VM.TextVM;

/// <summary>
/// TVM 操作类型。
/// </summary>
public enum TvmOperation : Byte
{
    /// <summary>
    /// 无操作。
    /// </summary>
    None = 0,

    /// <summary>
    /// 检查字符串是否存在。
    /// </summary>
    Exists = 1,

    /// <summary>
    /// 查找匹配位置。
    /// </summary>
    Find = 2,

    /// <summary>
    /// 查找并替换。
    /// </summary>
    Replace = 3,
}

/// <summary>
/// TVM 头部标志常量。
/// </summary>
public static class TvmFlags
{
    /// <summary>
    /// 表示正则表达式为纯字面量（无需编译 DFA）。
    /// </summary>
    public const UInt16 IsLiteral = 1;

    /// <summary>
    /// 表示存在字面量前缀，可用于快速过滤。
    /// </summary>
    public const UInt16 HasPrefix = 2;

    /// <summary>
    /// 表示存在捕获组。
    /// </summary>
    public const UInt16 HasCapture = 4;

    /// <summary>
    /// 表示使用 NFA（非确定性有限自动机）执行。
    /// </summary>
    public const UInt16 IsNfa = 8;
}

/// <summary>
/// TVM 二进制文件头部结构（32 字节）。
/// 前 4 字节为魔术标识 "TVM\0"，之后依次为各字段。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct TvmHeader
{
    /// <summary>
    /// 魔术标识 "TVM\0"。
    /// </summary>
    public static ReadOnlySpan<Byte> Magic => "TVM\0"u8;

    /// <summary>
    /// 头部总大小（魔术 4 字节 + 结构体 28 字节）。
    /// </summary>
    public const Int32 HeaderSize = 32;

    /// <summary>
    /// 版本号。
    /// </summary>
    public readonly UInt32 Version;

    /// <summary>
    /// 文本编码类型（对应 <see cref="TextEncoding"/> 枚举值）。
    /// </summary>
    public readonly Byte Encoding;

    /// <summary>
    /// 操作类型（对应 <see cref="TvmOperation"/> 枚举值）。
    /// </summary>
    public readonly Byte Operation;

    /// <summary>
    /// 标志位（参见 <see cref="TvmFlags"/>）。
    /// </summary>
    public readonly UInt16 Flags;

    /// <summary>
    /// 最小匹配长度。
    /// </summary>
    public readonly UInt32 MinMatchLen;

    /// <summary>
    /// 字面量前缀偏移量。
    /// </summary>
    public readonly UInt32 LiteralPrefixOffset;

    /// <summary>
    /// DFA 表偏移量。
    /// </summary>
    public readonly UInt32 DfaTableOffset;

    /// <summary>
    /// DFA 状态数量。
    /// </summary>
    public readonly UInt32 DfaStateCount;

    /// <summary>
    /// 文件总大小。
    /// </summary>
    public readonly UInt32 TotalSize;

    /// <summary>
    /// 创建新的 TVM 头部。
    /// </summary>
    /// <param name="version">版本号。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <param name="operation">操作类型。</param>
    /// <param name="flags">标志位。</param>
    /// <param name="minMatchLen">最小匹配长度。</param>
    /// <param name="literalPrefixOffset">字面量前缀偏移量。</param>
    /// <param name="dfaTableOffset">DFA 表偏移量。</param>
    /// <param name="dfaStateCount">DFA 状态数量。</param>
    /// <param name="totalSize">文件总大小。</param>
    public TvmHeader(
        UInt32 version,
        Byte encoding,
        Byte operation,
        UInt16 flags,
        UInt32 minMatchLen,
        UInt32 literalPrefixOffset,
        UInt32 dfaTableOffset,
        UInt32 dfaStateCount,
        UInt32 totalSize)
    {
        Version = version;
        Encoding = encoding;
        Operation = operation;
        Flags = flags;
        MinMatchLen = minMatchLen;
        LiteralPrefixOffset = literalPrefixOffset;
        DfaTableOffset = dfaTableOffset;
        DfaStateCount = dfaStateCount;
        TotalSize = totalSize;
    }

    /// <summary>
    /// 从字节切片中读取头部信息。
    /// </summary>
    /// <param name="data">包含完整 32 字节头部的字节切片。</param>
    /// <returns>解析后的头部结构。</returns>
    /// <exception cref="InvalidDataException">魔术标识不匹配或数据长度不足时引发。</exception>
    public static TvmHeader Read(ReadOnlySpan<Byte> data)
    {
        if (data.Length < HeaderSize)
        {
            throw new InvalidDataException(
                $"数据长度不足 {HeaderSize} 字节，无法读取头部");
        }

        if (data[0] != (Byte)'T' || data[1] != (Byte)'V' ||
            data[2] != (Byte)'M' || data[3] != (Byte)0)
        {
            throw new InvalidDataException("无效的魔术标识，头部数据已损坏");
        }

        return MemoryMarshal.Read<TvmHeader>(data.Slice(4));
    }

    /// <summary>
    /// 将头部信息写入字节切片。
    /// </summary>
    /// <param name="data">目标字节切片，长度必须至少为 32 字节。</param>
    /// <exception cref="ArgumentException">缓冲区长度不足 32 字节时引发。</exception>
    public void Write(Span<Byte> data)
    {
        if (data.Length < HeaderSize)
        {
            throw new ArgumentException(
                $"缓冲区长度不足 {HeaderSize} 字节", nameof(data));
        }

        "TVM\0"u8.CopyTo(data);
        MemoryMarshal.Write(data.Slice(4), in this);
    }
}
