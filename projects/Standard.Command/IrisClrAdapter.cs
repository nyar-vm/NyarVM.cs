namespace Std.Command;

/// <summary>
///     IrisApp 静态适配类，提供 Olymp.Iris 兼容的 CLI 入口，委托给 Sonic.Standard.Command 现有基础设施
/// </summary>
/// <remarks>
///     本文件为 Olymp.Iris → Sonic.Standard.Command 迁移桥接层。
///     由于命名空间 Sonic.Standard.Command 中已存在 <see cref="CommandRegistryBuilder" />，
///     本文件仅定义 IrisApp 入口及辅助类型，命令注册复用现有 CommandRegistryBuilder。
/// </remarks>
public static class IrisApp
{
    /// <summary>
    ///     运行命令注册并执行匹配的子命令
    /// </summary>
    /// <param name="args">命令行参数，args[0] 为子命令名</param>
    /// <param name="configurator">命令注册委托，用于注册子命令及其处理委托</param>
    /// <returns>退出码，0 表示成功</returns>
    public static int run(string[] args, Action<CommandRegistryBuilder> configurator)
    {
        // args 为空时直接返回成功
        if (args.Length == 0) return 0;

        var registry = new CommandRegistryBuilder();
        configurator(registry);

        var commandName = args[0];

        // 匹配已注册的命令
        var matched = registry._commands.FirstOrDefault(c =>
            string.Equals(c.name, commandName, StringComparison.OrdinalIgnoreCase));

        if (matched != null)
        {
            var commandArgs = args[1..];

            try
            {
                return CliArgumentParser.parse_and_execute(matched, commandArgs);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"错误: {ex.Message}");
                System.Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        System.Console.Error.WriteLine($"未知命令: {commandName}");
        return 1;
    }
}

/// <summary>
///     命令条目元数据，记录已注册命令的名称和处理委托
/// </summary>
/// <remarks>
///     对应 Olymp.Iris 中的 CommandEntry 概念。
///     实际命令注册通过 <see cref="CommandRegistryBuilder" /> 的 <see cref="CommandRegistryBuilder.Add" /> 方法，
///     内部使用 <see cref="Builder.BuiltCommandModel" /> 存储命令信息。
///     本类型用于替代 Olymp.Iris 中对应的命令条目类型。
/// </remarks>
public sealed class CommandEntry
{
    /// <summary>命令名称</summary>
    public string name { get; init; } = string.Empty;

    /// <summary>处理委托</summary>
    public Delegate? handler { get; init; }

    /// <summary>是否为异步命令</summary>
    public bool is_async { get; init; }
}

/// <summary>
///     命令执行结果
/// </summary>
public sealed class CommandResult
{
    /// <summary>是否执行成功</summary>
    public bool success { get; init; }

    /// <summary>退出码</summary>
    public int exit_code { get; init; }

    /// <summary>错误信息，成功时为 null</summary>
    public string? error { get; init; }

    /// <summary>
    ///     创建成功结果
    /// </summary>
    /// <param name="exitCode">退出码，默认为 0</param>
    public static CommandResult ok(int exitCode = 0)
    {
        return new CommandResult { success = true, exit_code = exitCode };
    }

    /// <summary>
    ///     创建失败结果
    /// </summary>
    /// <param name="error">错误描述</param>
    /// <param name="exitCode">退出码，默认为 1</param>
    public static CommandResult fail(string error, int exitCode = 1)
    {
        return new CommandResult { success = false, exit_code = exitCode, error = error };
    }
}