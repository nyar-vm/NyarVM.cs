using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     DeployCommand 的子命令测试。
///     测试 [Commands] 枚举标记的子命令分发。
/// </summary>
public class DeployCommandParseTests
{
    /// <summary>
    ///     测试子命令分发。
    /// </summary>
    [Fact]
    public void Parse_SubCommand_Push()
    {
        var cmd = DeployCommand.Parse(["push"]);

        Assert.Equal(DeployAction.push, cmd.action);
    }

    /// <summary>
    ///     测试子命令分发 Status。
    /// </summary>
    [Fact]
    public void Parse_SubCommand_Status()
    {
        var cmd = DeployCommand.Parse(["status"]);

        Assert.Equal(DeployAction.status, cmd.action);
    }

    /// <summary>
    ///     测试子命令与选项组合。
    /// </summary>
    [Fact]
    public void Parse_SubCommandWithOption()
    {
        var cmd = DeployCommand.Parse(["--env", "prod", "push"]);

        Assert.Equal("prod", cmd.environment);
        Assert.Equal(DeployAction.push, cmd.action);
    }

    /// <summary>
    ///     测试未知子命令。
    /// </summary>
    [Fact]
    public void TryParse_UnknownSubCommand_ReturnsFalse()
    {
        var result = DeployCommand.TryParse(["unknown"], out var cmd, out var error);

        Assert.False(result);
        Assert.Null(cmd);
        Assert.NotNull(error);
        Assert.Contains("未知子命令", error);
    }
}