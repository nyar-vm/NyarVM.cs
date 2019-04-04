using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     EchoCommand 的纯位置参数测试。
/// </summary>
public class EchoCommandParseTests
{
    /// <summary>
    ///     测试单个位置参数。
    /// </summary>
    [Fact]
    public void Parse_SinglePositionalArgument()
    {
        var cmd = EchoCommand.Parse(["hello"]);

        Assert.Equal("hello", cmd.message);
        Assert.Equal(1, cmd.count);
    }

    /// <summary>
    ///     测试多个位置参数。
    /// </summary>
    [Fact]
    public void Parse_MultiplePositionalArguments()
    {
        var cmd = EchoCommand.Parse(["hello", "3"]);

        Assert.Equal("hello", cmd.message);
        Assert.Equal(3, cmd.count);
    }

    /// <summary>
    ///     测试缺少必需参数。
    /// </summary>
    [Fact]
    public void TryParse_MissingRequired_ReturnsFalse()
    {
        var result = EchoCommand.TryParse([], out var cmd, out var error);

        Assert.False(result);
        Assert.Null(cmd);
        Assert.NotNull(error);
    }
}