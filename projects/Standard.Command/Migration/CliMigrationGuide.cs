namespace Std.Command.Migration;

/// <summary>
///     从其他 CLI 框架迁移到 Command 的对照指南，提供 System.CommandLine 和 McMaster 的等效写法
/// </summary>
public static class CliMigrationGuide
{
    /// <summary>
    ///     获取从 System.CommandLine 迁移到 Iris 的对照示例
    /// </summary>
    public static IReadOnlyList<MigrationExample> get_system_command_line_examples()
    {
        return new List<MigrationExample>
        {
            new()
            {
                title = "简单命令（无选项）",
                before_framework = "System.CommandLine",
                before_code = @"
var rootCommand = new RootCommand(""示例应用"");
rootCommand.SetHandler(() =>
{
    System.Console.WriteLine(""Hello World"");
});
return rootCommand.Invoke(args);",
                after_code = @"
CommandApp.Run(args, () =>
{
    System.Console.WriteLine(""Hello World"");
});"
            },
            new()
            {
                title = "带选项的命令",
                before_framework = "System.CommandLine",
                before_code = @"
var nameOption = new Option<string>(""--name"", ""你的名字"");
var countOption = new Option<int>(""--count"", () => 1, ""重复次数"");

var rootCommand = new RootCommand();
rootCommand.AddOption(nameOption);
rootCommand.AddOption(countOption);
rootCommand.SetHandler((name, count) =>
{
    for (var i = 0; i < count; i++)
        System.Console.WriteLine($""Hello, {name}"");
}, nameOption, countOption);
return rootCommand.Invoke(args);",
                after_code = @"
CommandApp.Run(args, (string name, int count = 1) =>
{
    for (var i = 0; i < count; i++)
        System.Console.WriteLine($""Hello, {name}"");
});"
            },
            new()
            {
                title = "子命令",
                before_framework = "System.CommandLine",
                before_code = @"
var rootCommand = new RootCommand();
var addCommand = new Command(""add"", ""添加项目"");
var removeCommand = new Command(""remove"", ""删除项目"");

rootCommand.AddCommand(addCommand);
rootCommand.AddCommand(removeCommand);
addCommand.SetHandler(() => System.Console.WriteLine(""添加""));
removeCommand.SetHandler(() => System.Console.WriteLine(""删除""));
return rootCommand.Invoke(args);",
                after_code = @"
var registry = new CommandRegistryBuilder();
registry.Add(""add"", () => System.Console.WriteLine(""添加""));
registry.Add(""remove"", () => System.Console.WriteLine(""删除""));
CommandApp.Run(args, registry);"
            },
            new()
            {
                title = "属性驱动命令（复杂命令推荐）",
                before_framework = "System.CommandLine",
                before_code = @"
[Command(""build"", Description = ""构建项目"")]
public class BuildCommand
{
    [Argument(Description = ""项目路径"")]
    public string Path { get; set; } = string.Empty;

    [Option(""--output"", Description = ""输出目录"")]
    public string Output { get; set; } = string.Empty;

    public int Execute()
    {
        System.Console.WriteLine($""构建 {Path} → {Output}"");
        return 0;
    }
}",
                after_code = @"
[Command(""build"", Description = ""构建项目"")]
public class BuildCommand : ICommand
{
    [Argument(Description = ""项目路径"")]
    public string Path { get; set; } = string.Empty;

    [Option(""--output"", Description = ""输出目录"")]
    public string Output { get; set; } = string.Empty;

    public Task ExecuteAsync(CommandContext context)
    {
        System.Console.WriteLine($""构建 {Path} → {Output}"");
        return Task.CompletedTask;
    }
}

// 入口调用
CommandApp.Run<BuildCommand>(args);"
            }
        };
    }

    /// <summary>
    ///     获取从 McMaster.Extensions.CommandLineUtils 迁移到 Iris 的对照示例
    /// </summary>
    public static IReadOnlyList<MigrationExample> get_mc_master_examples()
    {
        return new List<MigrationExample>
        {
            new()
            {
                title = "简单命令",
                before_framework = "McMaster",
                before_code = @"
var app = new CommandLineApplication();
app.OnExecute(() =>
{
    System.Console.WriteLine(""Hello"");
    return 0;
});
return app.Execute(args);",
                after_code = @"
CommandApp.Run(args, () =>
{
    System.Console.WriteLine(""Hello"");
});"
            },
            new()
            {
                title = "带选项的命令",
                before_framework = "McMaster",
                before_code = @"
var app = new CommandLineApplication();
var nameOpt = app.Option(""-n|--name <NAME>"", ""你的名字"", CommandOptionType.SingleValue);
app.OnExecute(() =>
{
    var name = nameOpt.Value();
    System.Console.WriteLine($""Hello, {name}"");
    return 0;
});
return app.Execute(args);",
                after_code = @"
CommandApp.Run(args, (string name) =>
{
    System.Console.WriteLine($""Hello, {name}"");
});"
            },
            new()
            {
                title = "参数（Positional Argument）",
                before_framework = "McMaster",
                before_code = @"
var app = new CommandLineApplication();
var fileArg = app.Argument(""file"", ""输入文件"");
app.OnExecute(() =>
{
    System.Console.WriteLine($""处理 {fileArg.Value}"");
    return 0;
});
return app.Execute(args);",
                after_code = @"
var registry = new CommandRegistryBuilder();
registry.Add(""process"", (CommandConfig config) =>
{
    config.AddArgument<string>(""file"", arg => arg.WithDescription(""输入文件"").Required());
    config.WithHandler((string file) =>
    {
        System.Console.WriteLine($""处理 {file}"");
    });
});
CommandApp.Run(args, registry);"
            },
            new()
            {
                title = "子命令（McMaster 风格）",
                before_framework = "McMaster",
                before_code = @"
var app = new CommandLineApplication();
app.Command(""add"", addCmd =>
{
    addCmd.OnExecute(() =>
    {
        System.Console.WriteLine(""添加项目"");
        return 0;
    });
});
app.Command(""remove"", removeCmd =>
{
    removeCmd.OnExecute(() =>
    {
        System.Console.WriteLine(""删除项目"");
        return 0;
    });
});
return app.Execute(args);",
                after_code = @"
var registry = new CommandRegistryBuilder();
registry.Add(""add"", () => System.Console.WriteLine(""添加项目""));
registry.Add(""remove"", () => System.Console.WriteLine(""删除项目""));
CommandApp.Run(args, registry);"
            }
        };
    }

    /// <summary>
    ///     获取所有迁移示例
    /// </summary>
    public static IReadOnlyList<MigrationExample> get_all_examples()
    {
        var all = new List<MigrationExample>();
        all.AddRange(get_system_command_line_examples());
        all.AddRange(get_mc_master_examples());
        return all;
    }
}