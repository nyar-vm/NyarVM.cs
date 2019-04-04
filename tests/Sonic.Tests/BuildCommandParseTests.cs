using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     BuildCommand 的多类型选项解析测试。
/// </summary>
public class BuildCommandParseTests
{
    /// <summary>
    ///     测试整数类型选项解析。
    /// </summary>
    [Fact]
    public void Parse_IntOption()
    {
        var cmd = BuildCommand.Parse(["--jobs", "8"]);

        Assert.Equal(8, cmd.jobs);
    }

    /// <summary>
    ///     测试短选项整数解析。
    /// </summary>
    [Fact]
    public void Parse_ShortIntOption()
    {
        var cmd = BuildCommand.Parse(["-j", "4", "-c", "Release"]);

        Assert.Equal(4, cmd.jobs);
        Assert.Equal("Release", cmd.config);
    }

    /// <summary>
    ///     测试布尔开关和字符串选项组合。
    /// </summary>
    [Fact]
    public void Parse_BoolAndStringOptions()
    {
        var cmd = BuildCommand.Parse(["--verbose", "--output", "bin/"]);

        Assert.True(cmd.verbose);
        Assert.Equal("bin/", cmd.output);
    }

    /// <summary>
    ///     测试所有默认值。
    /// </summary>
    [Fact]
    public void Parse_AllDefaults()
    {
        var cmd = BuildCommand.Parse([]);

        Assert.Equal("Debug", cmd.config);
        Assert.Equal(1, cmd.jobs);
        Assert.False(cmd.verbose);
        Assert.Null(cmd.output);
    }

    /// <summary>
    ///     测试 --version 标志。
    /// </summary>
    [Fact]
    public void TryParse_VersionFlag_ReturnsFalse()
    {
        var result = BuildCommand.TryParse(["--version"], out var cmd, out var error);

        Assert.False(result);
        Assert.Null(cmd);
    }
}