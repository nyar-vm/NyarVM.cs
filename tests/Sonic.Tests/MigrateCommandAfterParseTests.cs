using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     MigrateCommand 的 [AfterParse] 钩子测试。
/// </summary>
public class MigrateCommandAfterParseTests
{
    /// <summary>
    ///     测试解析完成后 AfterParse 方法被调用。
    /// </summary>
    [Fact]
    public void Parse_CallsAfterParseMethod()
    {
        var cmd = MigrateCommand.Parse(["--target", "v3", "--dry-run"]);

        Assert.Equal("v3", cmd.target);
        Assert.True(cmd.dry_run);
        Assert.True(cmd.after_parse_called);
    }

    /// <summary>
    ///     测试即使无选项也调用 AfterParse。
    /// </summary>
    [Fact]
    public void Parse_EmptyArgs_CallsAfterParse()
    {
        var cmd = MigrateCommand.Parse([]);

        Assert.True(cmd.after_parse_called);
    }
}