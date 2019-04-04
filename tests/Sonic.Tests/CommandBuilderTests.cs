using System.Globalization;

namespace Commander.Testing;

/// <summary>
///     命令构建器测试
/// </summary>
public sealed class CommandBuilderTests
{
    [Fact]
    public void Create_ShouldReturnBuilder()
    {
        var builder = CommandBuilder.create("myapp");
        Assert.NotNull(builder);
    }

    [Fact]
    public void AddCommand_with_handler_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("greet", cmd => cmd.with_handler(() => Console.WriteLine("hello")));
        });

        Assert.Null(exception);
    }

    [Fact]
    public void AddCommand_with_description_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("greet", cmd => cmd.with_description("打招呼命令"));
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CommandConfig_add_argument_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("calc", cmd =>
                {
                    cmd.add_argument<int>("value", arg => arg.with_description("数值").with_required());
                    cmd.with_handler((int value) => Console.WriteLine(value));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void ArgumentConfig_with_default_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("run", cmd =>
                {
                    cmd.add_argument<int>("count", arg => arg.with_default(42).with_description("次数"));
                    cmd.with_handler((int count) => Console.WriteLine(count));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CommandConfig_add_option_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("greet", cmd =>
                {
                    cmd.add_option<string>("name", opt => opt.with_short_name('n').with_description("姓名"));
                    cmd.with_handler((string name) => Console.WriteLine(name));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void OptionConfig_with_short_name_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("greet", cmd =>
                {
                    cmd.add_option<string>("output", opt => opt.with_short_name('o'));
                    cmd.with_handler((string output) => Console.WriteLine(output));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void OptionConfig_with_alias_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("greet", cmd =>
                {
                    cmd.add_option<string>("name", opt => opt.with_alias("n", "username"));
                    cmd.with_handler((string name) => Console.WriteLine(name));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CommandConfig_AddSubCommand_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("db",
                    cmd =>
                    {
                        cmd.add_sub_command("migrate", sub => sub.with_handler(() => Console.WriteLine("migrating")));
                    });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void MultipleArguments_ShouldBeConfigurable()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("copy", cmd =>
                {
                    cmd.add_argument<string>("src", arg => arg.with_required());
                    cmd.add_argument<string>("dst", arg => arg.with_required());
                    cmd.with_handler((string src, string dst) => Console.WriteLine($"{src} -> {dst}"));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void MultipleOptions_ShouldBeConfigurable()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("build", cmd =>
                {
                    cmd.add_option<string>("config", opt => opt.with_default("Debug"));
                    cmd.add_option<bool>("verbose", opt => opt.with_short_name('v'));
                    cmd.with_handler((string config, bool verbose) => { });
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void FlagOption_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("run", cmd =>
                {
                    cmd.add_option<bool>("force", opt => opt.with_short_name('f'));
                    cmd.with_handler((bool force) => Console.WriteLine(force));
                });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CommandBuilder_Run_UnknownCommand_ShouldPrintError()
    {
        var output = CaptureConsole(() =>
        {
            CommandBuilder.create("test")
                .add_command("known", cmd => cmd.with_handler(() => { }))
                .run(new[] { "unknown" });
        });

        Assert.Contains("未知命令", output);
    }

    [Fact]
    public void CommandBuilder_Run_EmptyArgs_ShouldPrintCommands()
    {
        var output = CaptureConsole(() =>
        {
            CommandBuilder.create("test", "测试工具")
                .add_command("greet", cmd => cmd.with_description("打招呼"))
                .run(Array.Empty<string>());
        });

        Assert.Contains("greet", output);
        Assert.Contains("打招呼", output);
    }

    [Fact]
    public void CommandConfig_Chaining_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            CommandBuilder.create("test")
                .add_command("all", cmd =>
                    cmd.with_description("全部")
                        .add_argument<int>("count", a => a.with_default(1).with_description("数量"))
                        .add_option<string>("name",
                            o => o.with_short_name('n').with_default("world").with_description("名字"))
                        .with_handler((int count, string name) => Console.WriteLine($"{count} {name}")));
        });

        Assert.Null(exception);
    }

    [Fact]
    public void HelpRenderer_Render_CommandInfo_ShouldNotBeEmpty()
    {
        var renderer = new HelpRenderer();
        var info = new CommandInfo
        {
            name = "test",
            description = "测试命令"
        };

        var output = renderer.render(info, CultureInfo.InvariantCulture);
        Assert.NotEmpty(output);
        Assert.Contains("test", output);
    }

    [Fact]
    public void HelpRenderer_RenderAll_ShouldNotBeEmpty()
    {
        var renderer = new HelpRenderer();
        var commands = new[]
        {
            new CommandInfo { name = "build", description = "构建项目" },
            new CommandInfo { name = "run", description = "运行项目" }
        };

        var output = renderer.render_all(commands, CultureInfo.InvariantCulture);
        Assert.NotEmpty(output);
        Assert.Contains("build", output);
        Assert.Contains("run", output);
    }

    // ================ M2 新增测试 ================

    /// <summary>
    ///     函数式模式：委托参数自动映射为选项，--name 和 --count 应正确执行
    /// </summary>
    [Fact]
    public void CommandApp_Run_Functional_Options_ShouldExecute()
    {
        string? capturedName = null;
        var capturedCount = 0;

        CommandApp.run(new[] { "--name", "Alice", "--count", "5" },
            (string name, int count) =>
            {
                capturedName = name;
                capturedCount = count;
            });

        Assert.Equal("Alice", capturedName);
        Assert.Equal(5, capturedCount);
    }

    /// <summary>
    ///     函数式模式：布尔标志应正确识别
    /// </summary>
    [Fact]
    public void CommandApp_Run_Functional_Flag_ShouldExecute()
    {
        var capturedVerbose = false;
        var capturedForce = false;

        CommandApp.run(new[] { "--verbose", "--force" },
            (bool verbose, bool force) =>
            {
                capturedVerbose = verbose;
                capturedForce = force;
            });

        Assert.True(capturedVerbose);
        Assert.True(capturedForce);
    }

    /// <summary>
    ///     函数式模式：--option=value 应正确解析
    /// </summary>
    [Fact]
    public void CommandApp_Run_Functional_EqualsSyntax_ShouldExecute()
    {
        var capturedName = "";

        CommandApp.run(new[] { "--name=Bob" },
            (string name) => { capturedName = name; });

        Assert.Equal("Bob", capturedName);
    }

    /// <summary>
    ///     CommandRegistryBuilder 多命令注册：应正确路由
    /// </summary>
    [Fact]
    public void CommandApp_Run_Registry_MultiCommand_ShouldRoute()
    {
        var captured = "";

        CommandApp.run(new[] { "greet", "--name", "Eve" },
            registry =>
            {
                registry.add("greet", (string name) => { captured = name; });

                registry.add("bye", () => { captured = "bye"; });
            });

        Assert.Equal("Eve", captured);
    }

    /// <summary>
    ///     属性驱动：CommandAttribute 的 Hidden 属性应存在
    /// </summary>
    [Fact]
    public void CommandAttribute_Hidden_ShouldExist()
    {
        var attr = new CommandAttribute("hidden-cmd", "隐藏命令")
        {
            hidden = true
        };

        Assert.True(attr.hidden);
    }

    /// <summary>
    ///     属性驱动：CommandAttribute 的 Alias 应存在
    /// </summary>
    [Fact]
    public void CommandAttribute_Alias_ShouldExist()
    {
        var attr = new CommandAttribute("main", "主命令")
        {
            alias = ["m", "primary"]
        };

        Assert.Contains("m", attr.alias);
        Assert.Contains("primary", attr.alias);
    }

    private static string CaptureConsole(Action action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var swOut = new StringWriter();
        using var swError = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swError);

        try
        {
            action();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        return swOut + swError;
    }
}

// ================ M2 测试模型 ================

/// <summary>
///     测试用：文件命令模型（属性驱动）
/// </summary>
[Command("test-cmd", "测试命令")]
public sealed class TestFileCommand
{
    [Argument(0, "文件路径")] public string FileName { get; set; } = string.Empty;
}

/// <summary>
///     测试用：工具命令模型（属性驱动），包含输出选项和详细模式标志
/// </summary>
[Command("tool", "工具命令")]
public sealed class TestToolCommand
{
    [Option('o', "output", "输出路径")] public string Output { get; set; } = string.Empty;

    [Option('v', "verbose", "详细输出")] public bool Verbose { get; set; }
}

/// <summary>
///     测试用：必填参数命令模型（属性驱动）
/// </summary>
[Command("required", "必填参数命令")]
public sealed class RequiredArgCommand
{
    [Argument(0, "必填值", required = true)] public string Value { get; set; } = string.Empty;
}

/// <summary>
///     测试用：仅短名称标志选项命令（属性驱动）
/// </summary>
[Command("silent", "静默命令")]
public sealed class ShortOptionCommand
{
    [Option('s', "silent", "静默模式")] public bool Silent { get; set; }
}

/// <summary>
///     测试用：父命令，含子命令属性（属性驱动）
/// </summary>
[Command("parent", "父命令")]
public sealed class ParentCommand
{
    [Subcommand] public ChildCommand? Child { get; set; }
}

/// <summary>
///     测试用：子命令（属性驱动）
/// </summary>
[Command("child", "子命令")]
public sealed class ChildCommand
{
    [Argument(0, "消息")] public string Message { get; set; } = string.Empty;
}

/// <summary>
///     测试用：剩余参数命令 — 收集全部位置参数到数组
/// </summary>
[Command("collect", "收集命令")]
public sealed class RemainingArgsCommand
{
    [Argument(0, "文件列表")] public string[] Files { get; set; } = [];
}

/// <summary>
///     测试用：混合参数命令 — 固定参数在前，剩余参数在后
/// </summary>
[Command("mixed", "混合参数命令")]
public sealed class MixedArgsCommand
{
    [Option('v', "verbose", "详细模式")] public bool Verbose { get; set; }

    [Argument(0, "源")] public string Source { get; set; } = string.Empty;

    [Argument(1, "剩余参数")] public string[] Remaining { get; set; } = [];
}