using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     GreetCommand 的 Parse/TryParse 解析测试。
///     测试 [Command] + [Argument] + [Option] 的基本解析能力。
///     命令名从 GreetCommand 自动推断为 greet，选项名从属性名自动推断。
/// </summary>
public class GreetCommandParseTests
{
    /// <summary>
    ///     测试解析位置参数和命名选项。
    /// </summary>
    [Fact]
    public void Parse_PositionalArgumentAndOption()
    {
        var cmd = GreetCommand.Parse(["World", "--greeting", "Hello"]);

        Assert.Equal("World", cmd.name);
        Assert.Equal("Hello", cmd.greeting);
        Assert.False(cmd.loud);
    }

    /// <summary>
    ///     测试解析布尔开关选项。
    /// </summary>
    [Fact]
    public void Parse_BoolFlag()
    {
        var cmd = GreetCommand.Parse(["World", "--loud"]);

        Assert.Equal("World", cmd.name);
        Assert.True(cmd.loud);
    }

    /// <summary>
    ///     测试短选项解析。
    /// </summary>
    [Fact]
    public void Parse_ShortOptions()
    {
        var cmd = GreetCommand.Parse(["World", "-g", "Hi", "-l"]);

        Assert.Equal("World", cmd.name);
        Assert.Equal("Hi", cmd.greeting);
        Assert.True(cmd.loud);
    }

    /// <summary>
    ///     测试缺少必需位置参数时解析失败。
    /// </summary>
    [Fact]
    public void TryParse_MissingRequiredArgument_ReturnsFalse()
    {
        var result = GreetCommand.TryParse(["--greeting", "Hello"], out var cmd, out var error);

        Assert.False(result);
        Assert.Null(cmd);
        Assert.NotNull(error);
        Assert.Contains("Name", error);
    }

    /// <summary>
    ///     测试未知选项时解析失败。
    /// </summary>
    [Fact]
    public void TryParse_UnknownOption_ReturnsFalse()
    {
        var result = GreetCommand.TryParse(["World", "--unknown"], out var cmd, out var error);

        Assert.False(result);
        Assert.Null(cmd);
        Assert.NotNull(error);
    }

    /// <summary>
    ///     测试 --help 标志。
    /// </summary>
    [Fact]
    public void TryParse_HelpFlag_ReturnsFalseWithHelpText()
    {
        var result = GreetCommand.TryParse(["--help"], out var cmd, out var error);

        Assert.False(result);
        Assert.Null(cmd);
        Assert.NotNull(error);
        Assert.Contains("greet", error);
    }

    /// <summary>
    ///     测试 Parse 在失败时抛出 CommandParseException。
    /// </summary>
    [Fact]
    public void Parse_Failure_ThrowsCommandParseException()
    {
        Assert.Throws<CommandParseException>(() =>
            GreetCommand.Parse(Array.Empty<string>()));
    }

    /// <summary>
    ///     测试默认值。
    /// </summary>
    [Fact]
    public void Parse_DefaultValues()
    {
        var cmd = GreetCommand.Parse(["World"]);

        Assert.Equal("World", cmd.name);
        Assert.Equal("你好", cmd.greeting);
        Assert.False(cmd.loud);
    }
}