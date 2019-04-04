namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// ReplaceTemplate 替换模板解析与应用的单元测试。
/// 覆盖 $N 捕获组引用、$$ 转义、混合 token 序列及实际替换。
/// </summary>
public class ReplaceTemplateTests
{
    /// <summary>
    /// 模板 "$1" 解析为单个 GroupRef(1) token。
    /// </summary>
    [Fact]
    public void Parse_GroupRef_ReturnsGroupRefToken()
    {
        ReplaceToken[] tokens = ReplaceTemplate.Parse("$1");

        Assert.Single(tokens);
        Assert.Equal(ReplaceTokenKind.GroupRef, tokens[0].Kind);
        Assert.Equal(1, tokens[0].GroupId);
    }

    /// <summary>
    /// 模板 "$$" 解析为单个字面量 "$" token。
    /// </summary>
    [Fact]
    public void Parse_LiteralDollar_ReturnsLiteralToken()
    {
        ReplaceToken[] tokens = ReplaceTemplate.Parse("$$");

        Assert.Single(tokens);
        Assert.Equal(ReplaceTokenKind.Literal, tokens[0].Kind);
        Assert.Equal([(Byte)'$'], tokens[0].LiteralBytes);
    }

    /// <summary>
    /// 模板 "hello $1 world" 解析为 Literal、GroupRef、Literal 三个 token。
    /// </summary>
    [Fact]
    public void Parse_MixedTokens_ReturnsCorrectSequence()
    {
        ReplaceToken[] tokens = ReplaceTemplate.Parse("hello $1 world");

        Assert.Equal(3, tokens.Length);
        Assert.Equal(ReplaceTokenKind.Literal, tokens[0].Kind);
        Assert.Equal(ReplaceTokenKind.GroupRef, tokens[1].Kind);
        Assert.Equal(1, tokens[1].GroupId);
        Assert.Equal(ReplaceTokenKind.Literal, tokens[2].Kind);
    }

    /// <summary>
    /// 使用 $0（全匹配）应用替换模板，正确提取匹配区间的内容。
    /// </summary>
    [Fact]
    public void Apply_WithCaptures_SubstitutesCorrectly()
    {
        ReplaceToken[] tokens = ReplaceTemplate.Parse("$0");
        Match match = new Match(1, 4);

        Byte[] input = [.. "xabcy"u8];
        Byte[] result = ReplaceTemplate.Apply(tokens, input, match, null);

        Assert.Equal("abc"u8.ToArray(), result);
    }

    /// <summary>
    /// 混合 token 应用：前缀 + 捕获组引用 + 后缀。
    /// </summary>
    [Fact]
    public void Apply_MixedTokens_ProducesCorrectOutput()
    {
        // 模板 "before$0after"
        ReplaceToken[] tokens = ReplaceTemplate.Parse("before$0after");
        Match match = new Match(0, 3);

        Byte[] input = [.. "abcxyz"u8];
        Byte[] result = ReplaceTemplate.Apply(tokens, input, match, null);

        // 预期输出 "beforeabcafter"
        Assert.Equal("beforeabcafter"u8.ToArray(), result);
    }
}
