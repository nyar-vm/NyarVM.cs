using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     命令名自动推断测试。
/// </summary>
public class CommandNameInferenceTests
{
    /// <summary>
    ///     测试 GreetCommand 推断为 greet。
    /// </summary>
    [Fact]
    public void GreetCommand_InferredAsGreet()
    {
        var help = GreetCommand.GetHelpText();
        Assert.Contains("greet", help);
    }

    /// <summary>
    ///     测试 BuildCommand 推断为 build。
    /// </summary>
    [Fact]
    public void BuildCommand_InferredAsBuild()
    {
        var help = BuildCommand.GetHelpText();
        Assert.Contains("build", help);
    }

    /// <summary>
    ///     测试 CalcCommand 推断为 calc。
    /// </summary>
    [Fact]
    public void CalcCommand_InferredAsCalc()
    {
        var help = CalcCommand.GetHelpText();
        Assert.Contains("calc", help);
    }
}