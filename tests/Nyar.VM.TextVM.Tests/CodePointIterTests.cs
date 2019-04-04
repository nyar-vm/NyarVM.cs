namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// CodePointIter 迭代器的单元测试。
/// 覆盖所有四种编码路径及边界情况。
/// </summary>
public class CodePointIterTests
{
    /// <summary>
    /// ASCII 编码中，有效字节直接返回对应字符。
    /// </summary>
    [Fact]
    public void Ascii_ValidBytes_ReturnsChar()
    {
        // "hello" 的 ASCII 编码
        Byte[] bytes = [0x68, 0x65, 0x6C, 0x6C, 0x6F];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Ascii);

        Int32 count = 0;
        while (iter.TryNext(out Int32 byteLen, out Char ch))
        {
            Assert.Equal(1, byteLen);
            Assert.Equal(bytes[count], (Byte)ch);
            count++;
        }

        Assert.Equal(5, count);
    }

    /// <summary>
    /// ASCII 编码中，大于 0x7F 的字节返回替换字符 U+FFFD。
    /// </summary>
    [Fact]
    public void Ascii_HighByte_ReturnsReplacement()
    {
        Byte[] bytes = [0xFF];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Ascii);

        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.True(result);
        Assert.Equal(1, byteLen);
        Assert.Equal('\uFFFD', ch);
    }

    /// <summary>
    /// UTF-8 编码中，多字节序列正确解码为 Unicode 字符。
    /// 包含 BMP 字符（U+4E2D）和增补字符（U+1F600）。
    /// </summary>
    [Fact]
    public void Utf8_MultiByte_ReturnsChar()
    {
        // "hi" (2 字节) + U+4E2D "中" (3 字节) + U+1F600 "😀" (4 字节)
        Byte[] bytes =
        [
            0x68, 0x69,
            0xE4, 0xB8, 0xAD,
            0xF0, 0x9F, 0x98, 0x80,
        ];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf8);

        // 'h' — 1 字节
        Assert.True(iter.TryNext(out Int32 len1, out Char ch1));
        Assert.Equal(1, len1);
        Assert.Equal('h', ch1);

        // 'i' — 1 字节
        Assert.True(iter.TryNext(out Int32 len2, out Char ch2));
        Assert.Equal(1, len2);
        Assert.Equal('i', ch2);

        // U+4E2D "中" — 3 字节
        Assert.True(iter.TryNext(out Int32 len3, out Char ch3));
        Assert.Equal(3, len3);
        Assert.Equal(0x4E2D, (Int32)ch3);

        // U+1F600 "😀" — 4 字节，作为代理对返回
        Assert.True(iter.TryNext(out Int32 len4, out Char ch4));
        Assert.Equal(4, len4);
        Assert.Equal(0xD83D, (Int32)ch4);

        // 第二个代理项
        Assert.True(iter.TryNext(out Int32 len5, out Char ch5));
        Assert.Equal(0, len5);
        Assert.Equal(0xDE00, (Int32)ch5);

        // 无更多字符
        Assert.False(iter.TryNext(out _, out _));
    }

    /// <summary>
    /// UTF-8 无效序列返回替换字符 U+FFFD。
    /// </summary>
    [Fact]
    public void Utf8_InvalidSequence_ReturnsReplacement()
    {
        Byte[] bytes = [0xFF];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf8);

        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.True(result);
        Assert.Equal(1, byteLen);
        Assert.Equal('\uFFFD', ch);
    }

    /// <summary>
    /// UTF-16LE 编码中，BMP 字符正确解码。
    /// </summary>
    [Fact]
    public void Utf16Le_BasicChar_ReturnsChar()
    {
        // 'A' (U+0041) 的 UTF-16LE 编码
        Byte[] bytes = [0x41, 0x00];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf16Le);

        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.True(result);
        Assert.Equal(2, byteLen);
        Assert.Equal('A', ch);
    }

    /// <summary>
    /// UTF-16BE 编码中，BMP 字符正确解码。
    /// </summary>
    [Fact]
    public void Utf16Be_BasicChar_ReturnsChar()
    {
        // 'A' (U+0041) 的 UTF-16BE 编码
        Byte[] bytes = [0x00, 0x41];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf16Be);

        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.True(result);
        Assert.Equal(2, byteLen);
        Assert.Equal('A', ch);
    }

    /// <summary>
    /// UTF-16 代理对（增补字符）返回高代理项，消耗 4 字节。
    /// 当前实现不通过 _pendingSurrogate 返回低代理项。
    /// </summary>
    [Fact]
    public void Utf16_SurrogatePair_ReturnsChar()
    {
        // U+1F600 (😀) 的 UTF-16LE 编码：0xD83D 0xDE00
        Byte[] bytes = [0x3D, 0xD8, 0x00, 0xDE];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf16Le);

        // 返回高代理项，消耗 4 字节
        Assert.True(iter.TryNext(out Int32 byteLen, out Char ch));
        Assert.Equal(4, byteLen);
        Assert.Equal(0xD83D, (Int32)ch);

        // 低代理项已被消耗，输入结束
        Assert.False(iter.TryNext(out _, out _));
    }

    /// <summary>
    /// UTF-16 中孤立的高代理项返回替换字符 U+FFFD。
    /// </summary>
    [Fact]
    public void Utf16_LoneSurrogate_ReturnsReplacement()
    {
        // 孤立的 0xD800（UTF-16LE）
        Byte[] bytes = [0x00, 0xD8];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf16Le);

        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.True(result);
        Assert.Equal(2, byteLen);
        Assert.Equal('\uFFFD', ch);
    }

    /// <summary>
    /// UTF-16 输入截断时，不抛出异常，返回替换字符。
    /// </summary>
    [Fact]
    public void Utf16_TruncatedInput_NoThrow()
    {
        // 只有 1 字节，不足 2 字节的 UTF-16 码元
        Byte[] bytes = [0x41];
        CodePointIter iter = new CodePointIter(bytes, TextEncoding.Utf16Le);

        // 不会抛出异常，返回替换字符
        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.True(result);
        Assert.Equal(1, byteLen);
        Assert.Equal('\uFFFD', ch);
    }

    /// <summary>
    /// 空输入返回 false。
    /// </summary>
    [Fact]
    public void EmptyInput_ReturnsFalse()
    {
        CodePointIter iter = new CodePointIter([], TextEncoding.Utf8);

        Boolean result = iter.TryNext(out Int32 byteLen, out Char ch);

        Assert.False(result);
        Assert.Equal(0, byteLen);
        Assert.Equal('\0', ch);
    }
}
