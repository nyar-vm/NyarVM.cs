using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     GetHelpText 生成测试。
/// </summary>
public class HelpTextTests
{
    /// <summary>
    ///     测试帮助文本包含命令名称和版本。
    /// </summary>
    [Fact]
    public void GetHelpText_ContainsNameAndVersion()
    {
        var help = GreetCommand.GetHelpText();

        Assert.Contains("greet", help);
        Assert.Contains("1.0.0", help);
    }

    /// <summary>
    ///     测试帮助文本包含从 summary 注释读取的描述。
    /// </summary>
    [Fact]
    public void GetHelpText_ContainsDescriptionFromSummary()
    {
        var help = GreetCommand.GetHelpText();

        Assert.Contains("打印问候语", help);
    }

    /// <summary>
    ///     测试帮助文本包含选项。
    /// </summary>
    [Fact]
    public void GetHelpText_ContainsOptions()
    {
        var help = GreetCommand.GetHelpText();

        Assert.Contains("--greeting", help);
        Assert.Contains("--loud", help);
    }

    /// <summary>
    ///     测试帮助文本包含参数。
    /// </summary>
    [Fact]
    public void GetHelpText_ContainsArguments()
    {
        var help = GreetCommand.GetHelpText();

        Assert.Contains("Name", help);
    }

    /// <summary>
    ///     测试 CalcCommand 帮助文本包含环境变量。
    /// </summary>
    [Fact]
    public void GetHelpText_ContainsEnvVars()
    {
        var help = CalcCommand.GetHelpText();

        Assert.Contains("CALC_PRECISION", help);
    }

    /// <summary>
    ///     测试 DeployCommand 帮助文本包含子命令。
    /// </summary>
    [Fact]
    public void GetHelpText_ContainsSubCommands()
    {
        var help = DeployCommand.GetHelpText();

        Assert.Contains("push", help);
        Assert.Contains("status", help);
    }
}