using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     GetCompletionScript 生成测试。
/// </summary>
public class CompletionScriptTests
{
    /// <summary>
    ///     测试 Bash 补全脚本生成。
    /// </summary>
    [Fact]
    public void GetCompletionScript_Bash_ContainsCommandName()
    {
        var script = GreetCommand.GetCompletionScript(ShellType.bash);

        Assert.Contains("greet", script);
        Assert.Contains("complete", script);
    }

    /// <summary>
    ///     测试 Zsh 补全脚本生成。
    /// </summary>
    [Fact]
    public void GetCompletionScript_Zsh_ContainsCommandName()
    {
        var script = GreetCommand.GetCompletionScript(ShellType.zsh);

        Assert.Contains("greet", script);
        Assert.Contains("#compdef", script);
    }

    /// <summary>
    ///     测试 Fish 补全脚本生成。
    /// </summary>
    [Fact]
    public void GetCompletionScript_Fish_ContainsCommandName()
    {
        var script = GreetCommand.GetCompletionScript(ShellType.fish);

        Assert.Contains("greet", script);
        Assert.Contains("complete", script);
    }

    /// <summary>
    ///     测试 PowerShell 补全脚本生成。
    /// </summary>
    [Fact]
    public void GetCompletionScript_PowerShell_ContainsCommandName()
    {
        var script = GreetCommand.GetCompletionScript(ShellType.power_shell);

        Assert.Contains("greet", script);
        Assert.Contains("Register-ArgumentCompleter", script);
    }

    /// <summary>
    ///     测试补全脚本包含选项。
    /// </summary>
    [Fact]
    public void GetCompletionScript_Bash_ContainsOptions()
    {
        var script = GreetCommand.GetCompletionScript(ShellType.bash);

        Assert.Contains("--greeting", script);
        Assert.Contains("--loud", script);
    }
}