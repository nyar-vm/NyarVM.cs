using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     验证可选位置参数在 SourceGen 解析路径下不会被误判为必填。
/// </summary>
public sealed class ProjectTestCommandParseTests
{
    /// <summary>
    ///     无位置参数时应成功解析，语义等价于 `legion test .` 里的省略项目路径。
    /// </summary>
    [Fact]
    public void Parse_WithoutProject_UsesOptionalPositionalArgument()
    {
        var cmd = ProjectTestCommand.Parse(["--target", "nyar"]);

        Assert.Null(cmd.project);
        Assert.Equal("nyar", cmd.target);
    }

    /// <summary>
    ///     TryParse 无位置参数时也应成功。
    /// </summary>
    [Fact]
    public void TryParse_WithoutProject_ReturnsTrue()
    {
        var result = ProjectTestCommand.TryParse(["--target", "nyar"], out var cmd, out var error);

        Assert.True(result);
        Assert.NotNull(cmd);
        Assert.Null(error);
        Assert.Null(cmd!.project);
        Assert.Equal("nyar", cmd.target);
    }
}
