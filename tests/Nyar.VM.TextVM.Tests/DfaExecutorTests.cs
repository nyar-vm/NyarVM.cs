namespace Nyar.VM.TextVM.Tests;

/// <summary>
/// DFA 执行器端到端测试。
/// </summary>
public class DfaExecutorTests
{
    [Fact]
    public void Alt_Matches() => Run("a|b", "xcay", 2, 3);

    [Fact]
    public void Star_MatchesLongest() => Run("abc*", "xabcccy", 1, 6);

    [Fact]
    public void CharClassPlus_Matches() => Run("[a-z]+", "hello 123 world", 0, 5);

    [Fact]
    public void Complex_Matches() => Run("a[b-c]+", "xaaabbc", 3, 7);

    [Fact]
    public void NoMatch()
    {
        Assert.Null(Compile("abc").Find([.. "xyz"u8]));
    }

    [Fact]
    public void ExistsWorks()
    {
        var q = Compile("hello");
        Assert.True(q.Exists([.. "hello world"u8]));
        Assert.True(q.Exists([.. "world hello"u8]));
        Assert.False(q.Exists([.. "xyz"u8]));
    }

    [Fact]
    public void FindAll_Returns3()
    {
        var matches = Compile("[0-9]+").FindAll([.. "a1b23c456"u8]).ToArray();
        Assert.Equal(3, matches.Length);
        Assert.Equal((1, 2), (matches[0].Start, matches[0].End));
        Assert.Equal((3, 5), (matches[1].Start, matches[1].End));
        Assert.Equal((6, 9), (matches[2].Start, matches[2].End));
    }

    [Fact]
    public void ReplaceWorks()
    {
        var q = StaticQuery.Compile("[0-9]+", TextEncoding.Utf8, TvmOperation.Replace);
        var r = q.Replace([.. "price: 123 and 456"u8], [.. "NUM"u8]);
        Assert.Equal("price: NUM and NUM", System.Text.Encoding.UTF8.GetString(r));
    }

    private static StaticQuery Compile(String p) =>
        StaticQuery.Compile(p, TextEncoding.Utf8, TvmOperation.Find);

    private static void Run(String pattern, String input, Int32 expStart, Int32 expEnd)
    {
        var m = Compile(pattern).Find(System.Text.Encoding.UTF8.GetBytes(input));
        Assert.NotNull(m);
        Assert.Equal(expStart, m!.Value.Start);
        Assert.Equal(expEnd, m.Value.End);
    }
}
