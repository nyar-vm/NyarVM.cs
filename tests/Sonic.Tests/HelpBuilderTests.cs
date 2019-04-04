namespace Commander.Testing;

/// <summary>
///     HelpBuilder 帮助文本构建器的单元测试。
/// </summary>
public class HelpBuilderTests
{
    /// <summary>
    ///     测试 append_header 生成正确的应用名和版本信息。
    /// </summary>
    [Fact]
    public void append_header_OutputsAppNameAndVersion()
    {
        var builder = new HelpBuilder();
        builder.append_header("MyApp", "1.2.3");
        var result = builder.ToString();

        Assert.Contains("MyApp v1.2.3", result);
    }

    /// <summary>
    ///     测试 append_description 在描述不为空时追加描述文本。
    /// </summary>
    [Fact]
    public void append_description_NonEmpty_OutputsDescription()
    {
        var builder = new HelpBuilder();
        builder.append_description("一个命令行工具");

        var result = builder.ToString();
        Assert.Contains("一个命令行工具", result);
    }

    /// <summary>
    ///     测试 append_description 在描述为空时不做任何输出。
    /// </summary>
    [Fact]
    public void append_description_Empty_OutputsNothing()
    {
        var builder = new HelpBuilder();
        builder.append_description(string.Empty);

        var result = builder.ToString();
        Assert.Empty(result);
    }

    /// <summary>
    ///     测试 append_usage 生成带命令名的用法说明。
    /// </summary>
    [Fact]
    public void append_usage_WithCommandName_OutputsUsageWithCommand()
    {
        var builder = new HelpBuilder();
        builder.append_usage("MyApp", "build", "[选项]");

        var result = builder.ToString();
        Assert.Contains("用法:", result);
        Assert.Contains("MyApp build [选项]", result);
    }

    /// <summary>
    ///     测试 append_usage 不传入命令名时只显示应用名。
    /// </summary>
    [Fact]
    public void append_usage_WithoutCommandName_OutputsUsageWithoutCommand()
    {
        var builder = new HelpBuilder();
        builder.append_usage("MyApp", null, "[命令] [选项]");

        var result = builder.ToString();
        Assert.Contains("MyApp [命令] [选项]", result);
    }

    /// <summary>
    ///     测试 append_commands 生成命令列表。
    /// </summary>
    [Fact]
    public void append_commands_WithCommands_OutputsCommandList()
    {
        var builder = new HelpBuilder();
        var commands = new Dictionary<string, string>
        {
            { "build", "构建项目" },
            { "run", "运行项目" }
        };
        builder.append_commands(commands);

        var result = builder.ToString();
        Assert.Contains("命令:", result);
        Assert.Contains("build", result);
        Assert.Contains("构建项目", result);
        Assert.Contains("run", result);
        Assert.Contains("运行项目", result);
    }

    /// <summary>
    ///     测试 append_commands 在命令集合为空时不做任何输出。
    /// </summary>
    [Fact]
    public void append_commands_Empty_OutputsNothing()
    {
        var builder = new HelpBuilder();
        builder.append_commands(new Dictionary<string, string>());

        var result = builder.ToString();
        Assert.Empty(result);
    }

    /// <summary>
    ///     测试 append_options 生成选项列表。
    /// </summary>
    [Fact]
    public void append_options_WithOptions_OutputsOptionList()
    {
        var builder = new HelpBuilder();
        var options = new List<(char, string, string)>
        {
            ('v', "verbose", "启用详细输出"),
            ('o', "output", "输出目录")
        };
        builder.append_options(options);

        var result = builder.ToString();
        Assert.Contains("选项:", result);
        Assert.Contains("-v, --verbose", result);
        Assert.Contains("启用详细输出", result);
        Assert.Contains("-o, --output", result);
        Assert.Contains("输出目录", result);
    }

    /// <summary>
    ///     测试 append_options 在选项集合为空时不做任何输出。
    /// </summary>
    [Fact]
    public void append_options_Empty_OutputsNothing()
    {
        var builder = new HelpBuilder();
        builder.append_options(new List<(char, string, string)>());

        var result = builder.ToString();
        Assert.Empty(result);
    }

    /// <summary>
    ///     测试 append_arguments 生成位置参数列表。
    /// </summary>
    [Fact]
    public void append_arguments_WithArguments_OutputsArgumentList()
    {
        var builder = new HelpBuilder();
        var arguments = new List<(string, string, bool)>
        {
            ("input", "输入文件路径", true),
            ("output", "输出文件路径", false)
        };
        builder.append_arguments(arguments);

        var result = builder.ToString();
        Assert.Contains("参数:", result);
        Assert.Contains("<input> (必填)", result);
        Assert.Contains("<output> (可选)", result);
        Assert.Contains("输入文件路径", result);
        Assert.Contains("输出文件路径", result);
    }

    /// <summary>
    ///     测试 append_arguments 在参数集合为空时不做任何输出。
    /// </summary>
    [Fact]
    public void append_arguments_Empty_OutputsNothing()
    {
        var builder = new HelpBuilder();
        builder.append_arguments(new List<(string, string, bool)>());

        var result = builder.ToString();
        Assert.Empty(result);
    }

    /// <summary>
    ///     测试 append_sub_commands 生成子命令列表。
    /// </summary>
    [Fact]
    public void append_sub_commands_WithSubCommands_OutputsSubCommandList()
    {
        var builder = new HelpBuilder();
        var subCommands = new List<(string, string?, string[]?)>
        {
            ("init", "初始化项目", null),
            ("add", "添加依赖", ["a"])
        };
        builder.append_sub_commands(subCommands);

        var result = builder.ToString();
        Assert.Contains("子命令:", result);
        Assert.Contains("init", result);
        Assert.Contains("初始化项目", result);
        Assert.Contains("add", result);
        Assert.Contains("(a)", result);
    }

    /// <summary>
    ///     测试 append_sub_commands 在子命令集合为空时不做任何输出。
    /// </summary>
    [Fact]
    public void append_sub_commands_Empty_OutputsNothing()
    {
        var builder = new HelpBuilder();
        builder.append_sub_commands(new List<(string, string?, string[]?)>());

        var result = builder.ToString();
        Assert.Empty(result);
    }

    /// <summary>
    ///     测试 append_environment_variables 生成环境变量映射列表。
    /// </summary>
    [Fact]
    public void append_environment_variables_WithEnvVars_OutputsEnvVarList()
    {
        var builder = new HelpBuilder();
        var envVars = new List<(string, string?)>
        {
            ("config", "APP_CONFIG"),
            ("verbose", "APP_VERBOSE")
        };
        builder.append_environment_variables(envVars);

        var result = builder.ToString();
        Assert.Contains("环境变量:", result);
        Assert.Contains("--config", result);
        Assert.Contains("APP_CONFIG", result);
        Assert.Contains("--verbose", result);
        Assert.Contains("APP_VERBOSE", result);
    }

    /// <summary>
    ///     测试 append_environment_variables 在环境变量集合为空时不做任何输出。
    /// </summary>
    [Fact]
    public void append_environment_variables_Empty_OutputsNothing()
    {
        var builder = new HelpBuilder();
        builder.append_environment_variables(new List<(string, string?)>());

        var result = builder.ToString();
        Assert.Empty(result);
    }

    /// <summary>
    ///     测试链式调用构建完整帮助文本。
    /// </summary>
    [Fact]
    public void ChainedCalls_BuildsCompleteHelpText()
    {
        var builder = new HelpBuilder();
        builder
            .append_header("Tool", "2.0.0")
            .append_description("一个实用的命令行工具")
            .append_usage("Tool", null, "<command> [选项]")
            .append_commands(new Dictionary<string, string> { { "start", "启动服务" } })
            .append_options(new List<(char, string, string)> { ('h', "help", "显示帮助") });

        var result = builder.ToString();

        Assert.Contains("Tool v2.0.0", result);
        Assert.Contains("一个实用的命令行工具", result);
        Assert.Contains("用法:", result);
        Assert.Contains("命令:", result);
        Assert.Contains("选项:", result);
    }

    /// <summary>
    ///     测试 clear 方法清空构建器内容。
    /// </summary>
    [Fact]
    public void clear_ResetsBuilder()
    {
        var builder = new HelpBuilder();
        builder.append_header("Test", "1.0.0");

        Assert.NotEmpty(builder.ToString());

        builder.clear();
        Assert.Empty(builder.ToString());
    }
}