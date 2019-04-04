using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     选项名自动推断测试。
/// </summary>
public class OptionNameInferenceTests
{
    /// <summary>
    ///     测试属性名自动推断为 kebab-case 选项名。
    /// </summary>
    [Fact]
    public void PropertyName_InferredAsKebabCase()
    {
        var help = BuildCommand.GetHelpText();
        Assert.Contains("--config", help);
        Assert.Contains("--jobs", help);
        Assert.Contains("--verbose", help);
        Assert.Contains("--output", help);
    }

    /// <summary>
    ///     测试 DryRun 属性推断为 --dry-run。
    /// </summary>
    [Fact]
    public void DryRunProperty_InferredAsDryRun()
    {
        var help = MigrateCommand.GetHelpText();
        Assert.Contains("--dry-run", help);
    }
}