using System.Runtime.InteropServices;

namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// TvmHeader 序列化与反序列化的单元测试。
/// </summary>
public class TvmHeaderTests
{
    /// <summary>
    /// 写入后再读取，应恢复出等价的字节序列。
    /// </summary>
    [Fact]
    public void WriteRead_RoundTrip_ReturnsEquivalentFields()
    {
        // 构造包含已知字段值的 32 字节头部
        Span<Byte> original = new Byte[TvmHeader.HeaderSize];
        "TVM\0"u8.CopyTo(original);

        UInt32 version = 42;
        Byte encoding = (Byte)TextEncoding.Utf8;
        Byte operation = (Byte)TvmOperation.Find;
        UInt16 flags = TvmFlags.HasPrefix | TvmFlags.HasCapture;
        UInt32 minMatchLen = 7;
        UInt32 literalPrefixOffset = 32;
        UInt32 dfaTableOffset = 128;
        UInt32 dfaStateCount = 25;
        UInt32 totalSize = 512;

        Int32 offset = 4;
        MemoryMarshal.Write(original.Slice(offset), in version);
        offset += 4;
        original[offset++] = encoding;
        original[offset++] = operation;
        MemoryMarshal.Write(original.Slice(offset), in flags);
        offset += 2;
        MemoryMarshal.Write(original.Slice(offset), in minMatchLen);
        offset += 4;
        MemoryMarshal.Write(original.Slice(offset), in literalPrefixOffset);
        offset += 4;
        MemoryMarshal.Write(original.Slice(offset), in dfaTableOffset);
        offset += 4;
        MemoryMarshal.Write(original.Slice(offset), in dfaStateCount);
        offset += 4;
        MemoryMarshal.Write(original.Slice(offset), in totalSize);

        // 读取
        TvmHeader header = TvmHeader.Read(original);

        // 写入新缓冲区
        Span<Byte> rewritten = new Byte[TvmHeader.HeaderSize];
        header.Write(rewritten);

        // 写入结果应与原始数据一致
        Assert.True(original.SequenceEqual(rewritten));
    }

    /// <summary>
    /// 无效的魔术标识应引发 InvalidDataException。
    /// </summary>
    [Fact]
    public void BadMagic_ThrowsInvalidDataException()
    {
        // 长度 >= 32 字节，但魔术标识为 "XVM\0" 而非 "TVM\0"
        Byte[] badData = new Byte[TvmHeader.HeaderSize];
        badData[0] = (Byte)'X';
        badData[1] = (Byte)'V';
        badData[2] = (Byte)'M';
        badData[3] = 0;

        Assert.Throws<InvalidDataException>(() => TvmHeader.Read(badData));
    }

    /// <summary>
    /// 数据长度不足 32 字节时应引发 InvalidDataException。
    /// </summary>
    [Fact]
    public void ShortData_ThrowsInvalidDataException()
    {
        // 仅 4 字节 "TVM\0"，不足 32 字节
        Byte[] shortData = [0x54, 0x56, 0x4D, 0x00];

        Assert.Throws<InvalidDataException>(() => TvmHeader.Read(shortData));
    }
}
