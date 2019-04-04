namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// TwoWayMatcher 双向字符串匹配算法的单元测试。
/// 覆盖 Byte 和 UInt16 两种切片重载。
/// </summary>
public class TwoWayMatcherTests
{
    /// <summary>
    /// 字面量搜索：存在匹配时返回正确的起始位置。
    /// </summary>
    [Fact]
    public void IndexOf_Found_ReturnsPosition()
    {
        Byte[] text = [.. "hello world"u8];
        Byte[] pattern = [.. "world"u8];

        Int32 result = TwoWayMatcher.IndexOf(text, pattern);

        Assert.Equal(6, result);
    }

    /// <summary>
    /// 字面量搜索：不存在匹配时返回 -1。
    /// </summary>
    [Fact]
    public void IndexOf_NotFound_ReturnsMinusOne()
    {
        Byte[] text = [.. "hello world"u8];
        Byte[] pattern = [.. "xyz"u8];

        Int32 result = TwoWayMatcher.IndexOf(text, pattern);

        Assert.Equal(-1, result);
    }

    /// <summary>
    /// IndexOfAll 返回所有不重叠匹配的起始位置。
    /// </summary>
    [Fact]
    public void IndexOfAll_MultipleMatches_ReturnsAllPositions()
    {
        Byte[] text = [.. "ababab"u8];
        Byte[] pattern = [.. "ab"u8];

        Int32[] results = TwoWayMatcher.IndexOfAll(text, pattern);

        Assert.Equal([0, 2, 4], results);
    }

    /// <summary>
    /// 空模式应返回 0。
    /// </summary>
    [Fact]
    public void IndexOf_EmptyPattern_ReturnsZero()
    {
        Byte[] text = [.. "abc"u8];
        Byte[] pattern = [];

        Int32 result = TwoWayMatcher.IndexOf(text, pattern);

        Assert.Equal(0, result);
    }

    /// <summary>
    /// UInt16 重载：存在匹配时返回正确的起始位置。
    /// </summary>
    [Fact]
    public void IndexOf_UInt16_Found_ReturnsPosition()
    {
        UInt16[] text = [(UInt16)'h', (UInt16)'e', (UInt16)'l', (UInt16)'l', (UInt16)'o'];
        UInt16[] pattern = [(UInt16)'l', (UInt16)'l'];

        Int32 result = TwoWayMatcher.IndexOf(text.AsSpan(), pattern.AsSpan());

        Assert.Equal(2, result);
    }

    /// <summary>
    /// 模式长度大于文本长度时返回 -1。
    /// </summary>
    [Fact]
    public void IndexOf_PatternLongerThanText_ReturnsMinusOne()
    {
        Byte[] text = [.. "abc"u8];
        Byte[] pattern = [.. "abcd"u8];

        Int32 result = TwoWayMatcher.IndexOf(text, pattern);

        Assert.Equal(-1, result);
    }
}
