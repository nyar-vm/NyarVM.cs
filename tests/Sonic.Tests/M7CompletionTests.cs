namespace Commander.Testing;

/// <summary>
///     M7 Shell 自动补全系统测试
/// </summary>
public sealed class M7CompletionTests
{
    /// <summary>
    ///     生成 Bash 补全脚本：应包含 complete -F 和命令名
    /// </summary>
    [Fact]
    public void GenerateBash_BasicCommand_ShouldContainCompletionHook()
    {
        var commands = new[]
        {
            new CommandInfo { name = "greet", description = "打招呼" },
            new CommandInfo { name = "build", description = "构建" }
        };

        var script = CompletionScriptGenerator.generate("bash", "myapp", commands);

        Assert.Contains("_myapp()", script);
        Assert.Contains("complete -F _myapp myapp", script);
        Assert.Contains("greet", script);
        Assert.Contains("build", script);
    }

    /// <summary>
    ///     生成 Bash 补全脚本：应包含选项名
    /// </summary>
    [Fact]
    public void GenerateBash_WithOptions_ShouldContainOptionNames()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "greet",
                description = "打招呼",
                options = new List<OptionInfo>
                {
                    new() { long_name = "name", short_name = 'n', description = "姓名" },
                    new() { long_name = "verbose", description = "详细模式", is_flag = true }
                }
            }
        };

        var script = CompletionScriptGenerator.generate("bash", "tool", commands);

        Assert.Contains("greet", script);
        Assert.Contains("cmd_path", script);
    }

    /// <summary>
    ///     生成 Zsh 补全脚本：应包含 #compdef 和 _describe
    /// </summary>
    [Fact]
    public void GenerateZsh_BasicCommand_ShouldContainZshPragma()
    {
        var commands = new[]
        {
            new CommandInfo { name = "serve", description = "启动服务" }
        };

        var script = CompletionScriptGenerator.generate("zsh", "myapp", commands);

        Assert.Contains("#compdef myapp", script);
        Assert.Contains("_myapp()", script);
        Assert.Contains("_describe", script);
        Assert.Contains("serve", script);
    }

    /// <summary>
    ///     生成 Zsh 补全脚本：应包含选项定义
    /// </summary>
    [Fact]
    public void GenerateZsh_WithOptions_ShouldContainOptionSpecs()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "run",
                description = "运行",
                options = new List<OptionInfo>
                {
                    new() { long_name = "config", short_name = 'c', description = "配置文件" }
                }
            }
        };

        var script = CompletionScriptGenerator.generate("zsh", "cli", commands);

        Assert.Contains("(-c --config)", script);
        Assert.Contains("配置文件", script);
    }

    /// <summary>
    ///     生成 PowerShell 补全脚本：应包含 Register-ArgumentCompleter
    /// </summary>
    [Fact]
    public void GeneratePowerShell_BasicCommand_ShouldContainRegisterCompleter()
    {
        var commands = new[]
        {
            new CommandInfo { name = "deploy", description = "部署" }
        };

        var script = CompletionScriptGenerator.generate("powershell", "myapp", commands);

        Assert.Contains("Register-ArgumentCompleter -Native -CommandName myapp", script);
        Assert.Contains("deploy", script);
    }

    /// <summary>
    ///     生成 PowerShell 补全脚本：应包含选项等待补全
    /// </summary>
    [Fact]
    public void GeneratePowerShell_WithOptions_ShouldContainOptionArrays()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "build",
                description = "构建",
                options = new List<OptionInfo>
                {
                    new() { long_name = "output", short_name = 'o', description = "输出目录" }
                }
            }
        };

        var script = CompletionScriptGenerator.generate("powershell", "tool", commands);

        Assert.Contains("build", script);
        Assert.Contains("--output", script);
        Assert.Contains("-o", script);
    }

    /// <summary>
    ///     不支持的 Shell 类型应抛出 ArgumentException
    /// </summary>
    [Fact]
    public void Generate_UnsupportedShell_ShouldThrowArgumentException()
    {
        var commands = new[] { new CommandInfo { name = "test" } };

        var ex = Assert.Throws<ArgumentException>(() =>
            CompletionScriptGenerator.generate("csh", "myapp", commands));

        Assert.Contains("不支持", ex.Message);
        Assert.Contains("csh", ex.Message);
    }

    /// <summary>
    ///     GetSupportedShells 应返回 bash/zsh/powershell/fish
    /// </summary>
    [Fact]
    public void GetSupportedShells_ShouldReturnFourShells()
    {
        var shells = CommandApp.get_supported_shells();

        Assert.Equal(4, shells.Count);
        Assert.Contains("bash", shells);
        Assert.Contains("zsh", shells);
        Assert.Contains("powershell", shells);
    }

    /// <summary>
    ///     CommandApp.GenerateCompletionScript 通过 CommandInfo 数组生成
    /// </summary>
    [Fact]
    public void CommandApp_GenerateCompletionScript_WithCommandInfos_ShouldNotBeEmpty()
    {
        var result = CommandApp.generate_completion_script("bash", "myapp",
            new CommandInfo { name = "greet", description = "打招呼" },
            new CommandInfo { name = "build", description = "构建项目" });

        Assert.NotEmpty(result);
        Assert.Contains("greet", result);
        Assert.Contains("build", result);
    }

    /// <summary>
    ///     CommandApp.GenerateCompletionScript 通过 CommandRegistryBuilder 生成
    /// </summary>
    [Fact]
    public void CommandApp_GenerateCompletionScript_WithRegistry_ShouldNotBeEmpty()
    {
        var registry = new CommandRegistryBuilder();
        registry.add("greet", () => Console.WriteLine("hello"));
        registry.add("build", () => Console.WriteLine("building"));

        var result = CommandApp.generate_completion_script("zsh", "myapp", registry);

        Assert.NotEmpty(result);
        Assert.Contains("greet", result);
        Assert.Contains("build", result);
    }

    /// <summary>
    ///     生成 Bash 脚本：大小写不敏感匹配 Shell 名称
    /// </summary>
    [Fact]
    public void Generate_ShellNameCaseInsensitive_ShouldWork()
    {
        var commands = new[] { new CommandInfo { name = "test" } };

        var lower = CompletionScriptGenerator.generate("bash", "app", commands);
        var upper = CompletionScriptGenerator.generate("BASH", "app", commands);
        var mixed = CompletionScriptGenerator.generate("Bash", "app", commands);

        Assert.Equal(lower, upper);
        Assert.Equal(lower, mixed);
    }

    /// <summary>
    ///     生成 Bash 脚本：应包含别名
    /// </summary>
    [Fact]
    public void GenerateBash_WithAliases_ShouldContainAliasNames()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "list",
                description = "列表",
                options = new List<OptionInfo>
                {
                    new()
                    {
                        long_name = "format",
                        description = "格式",
                        aliases = new List<string> { "fmt", "display" }
                    }
                }
            }
        };

        var script = CompletionScriptGenerator.generate("bash", "tool", commands);

        Assert.Contains("list", script);
        Assert.Contains("format", script);
    }

    /// <summary>
    ///     生成 Zsh 脚本：描述中包含单引号应正确转义
    /// </summary>
    [Fact]
    public void GenerateZsh_DescriptionWithSingleQuote_ShouldBeEscaped()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "greet",
                description = "打招呼'你好'"
            }
        };

        var script = CompletionScriptGenerator.generate("zsh", "app", commands);

        Assert.Contains("打招呼'\\''你好'\\''", script);
    }

    /// <summary>
    ///     空命令列表：生成脚本不应抛异常
    /// </summary>
    [Fact]
    public void Generate_EmptyCommands_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
            CompletionScriptGenerator.generate("bash", "app"));

        Assert.Null(exception);
    }

    /// <summary>
    ///     带子命令的补全：PowerShell 应包含子命令
    /// </summary>
    [Fact]
    public void GeneratePowerShell_WithSubCommands_ShouldContainSubArray()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "db",
                description = "数据库",
                sub_commands = new List<CommandInfo>
                {
                    new() { name = "migrate", description = "迁移" },
                    new() { name = "seed", description = "填充数据" }
                }
            }
        };

        var script = CompletionScriptGenerator.generate("powershell", "cli", commands);

        Assert.Contains("db", script);
        Assert.Contains("migrate", script);
        Assert.Contains("seed", script);
    }

    /// <summary>
    ///     Bash 子命令补全：应包含 case 匹配逻辑
    /// </summary>
    [Fact]
    public void GenerateBash_WithSubCommands_ShouldContainCaseSwitch()
    {
        var commands = new[]
        {
            new CommandInfo
            {
                name = "config",
                description = "配置管理",
                sub_commands = new List<CommandInfo>
                {
                    new() { name = "get", description = "获取" },
                    new() { name = "set", description = "设置" }
                }
            }
        };

        var script = CompletionScriptGenerator.generate("bash", "tool", commands);

        Assert.Contains("case", script);
        Assert.Contains("config)", script);
        Assert.Contains("get", script);
        Assert.Contains("set", script);
    }
}