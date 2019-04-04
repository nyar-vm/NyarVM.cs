namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// StaticQuery 完整编译管线的单元测试。
/// 覆盖字面量和无效模式路径。
/// </summary>
public class StaticQueryTests
{
    /// <summary>
    /// 单字符字面量模式 "a" 通过 LiteralExecutor 路径正确匹配。
    /// </summary>
    [Fact]
    public void Literal_IsMatch_ReturnsTrue()
    {
        StaticQuery query = StaticQuery.Compile(
            "a",
            TextEncoding.Utf8,
            TvmOperation.Exists);

        Byte[] input = [.. "abc"u8];

        Assert.True(query.Exists(input));
    }

    /// <summary>
    /// 未闭合的组应引发 ArgumentException。
    /// </summary>
    [Fact]
    public void InvalidPattern_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StaticQuery.Compile(
                "(abc",
                TextEncoding.Utf8,
                TvmOperation.Exists));
    }
}
