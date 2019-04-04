using System.Reflection;
using Core.Command;
using ExitCode = Core.Terminal.ExitCode;
using Microsoft.Extensions.DependencyInjection;
using Std.Command.Builder;
using Std.Command.Completion;
using Std.Command.Help;
using Std.Command.Hosting;
using Std.Command.Metadata;
using Std.Command.Middleware;
using Std.Console;
using ICommand = Core.Command.ICommand;

namespace Std.Command;

/// <summary>
///     Command CLI 应用入口，提供多种调用模式，支持中间件管道和生命周期钩子
/// </summary>
public static class CommandApp
{
    private static readonly List<ILifecycleHook> _lifecycle_hooks = [];
    private static string _app_name = string.Empty;
    private static string _app_description = string.Empty;
    private static string _app_version = string.Empty;
    private static CommandConfiguration? _configuration;
    private static IServiceProvider? _services;

    /// <summary>
    ///     全局中间件管道
    /// </summary>
    public static MiddlewarePipeline middleware_pipeline { get; } = new();

    /// <summary>
    ///     设置应用名称（用于帮助文本和版本显示）
    /// </summary>
    /// <param name="name">应用名称</param>
    /// <returns>应用构建器</returns>
    public static CommandAppBuilder with_name(string name)
    {
        _app_name = name;
        return new CommandAppBuilder();
    }

    /// <summary>
    ///     设置应用描述
    /// </summary>
    /// <param name="description">应用描述</param>
    public static void with_description(string description)
    {
        _app_description = description;
    }

    /// <summary>
    ///     设置应用版本号
    /// </summary>
    /// <param name="version">版本号</param>
    public static void with_version(string version)
    {
        _app_version = version;
    }

    /// <summary>
    ///     注册中间件类型到全局管道
    /// </summary>
    /// <typeparam name="T">实现了 ICommandMiddleware 的类型</typeparam>
    public static void use_middleware<T>() where T : ICommandMiddleware, new()
    {
        middleware_pipeline.use<T>();
    }

    /// <summary>
    ///     注册中间件实例到全局管道
    /// </summary>
    /// <param name="middleware">中间件实例</param>
    public static void use_middleware(ICommandMiddleware middleware)
    {
        middleware_pipeline.use(middleware);
    }

    /// <summary>
    ///     注册 Lambda 中间件到全局管道
    /// </summary>
    /// <param name="middleware">中间件委托</param>
    public static void use_middleware(
        Func<ICommandContext, Func<Task<ExitCode>>, CancellationToken, Task<ExitCode>> middleware)
    {
        middleware_pipeline.use(middleware);
    }

    /// <summary>
    ///     注册生命周期钩子
    /// </summary>
    /// <param name="hook">生命周期钩子实例</param>
    public static void use_lifecycle_hook(ILifecycleHook hook)
    {
        _lifecycle_hooks.Add(hook);
    }

    /// <summary>
    ///     设置应用配置
    /// </summary>
    /// <param name="configuration">配置实例</param>
    public static void set_configuration(CommandConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    ///     获取应用配置
    /// </summary>
    /// <returns>配置实例，未设置时返回 null</returns>
    public static CommandConfiguration? get_configuration()
    {
        return _configuration;
    }

    /// <summary>
    ///     设置服务提供者
    /// </summary>
    /// <param name="services">服务提供者</param>
    public static void set_services(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    ///     获取服务提供者
    /// </summary>
    /// <returns>服务提供者，未设置时返回 null</returns>
    public static IServiceProvider? get_services()
    {
        return _services;
    }

    /// <summary>
    ///     解析服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public static T? resolve_service<T>() where T : class
    {
        return _services?.GetService<T>();
    }

    /// <summary>
    ///     属性驱动方式：解析并执行指定命令类型
    /// </summary>
    /// <typeparam name="T">命令类型（实现 ICommand 且具有 new()）</typeparam>
    /// <param name="args">命令行参数</param>
    /// <returns>退出码</returns>
    public static int run<T>(string[] args) where T : ICommand, new()
    {
        return run_async<T>(args).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     属性驱动方式：异步解析并执行指定命令类型
    /// </summary>
    /// <typeparam name="T">命令类型（实现 ICommand 且具有 new()）</typeparam>
    /// <param name="args">命令行参数</param>
    /// <returns>退出码</returns>
    public static async Task<int> run_async<T>(string[] args) where T : ICommand, new()
    {
        try
        {
            var appName = resolve_app_name();

            // 处理 --help / -h（无参数或仅帮助标志）
            if (args.Length == 0 || args[0] is "--help" or "-h")
            {
                var helpText = generate_attribute_help<T>(appName);
                System.Console.WriteLine(helpText);
                return (int)ExitCode.Success;
            }

            // 处理 --version / -v
            if (args[0] is "--version" or "-v")
            {
                var version = string.IsNullOrEmpty(_app_version) ? "1.0.0" : _app_version;
                System.Console.WriteLine($"{appName} v{version}");
                return (int)ExitCode.Success;
            }

            // 使用 CliArgumentParser 进行属性驱动解析
            if (!CliArgumentParser.try_parse<T>(args, out var command, out var errors))
            {
                await System.Console.Error.WriteLineAsync(errors.FirstOrDefault() ?? "解析失败");
                return (int)ExitCode.InvalidArgs;
            }

            // 检测子命令路由：如果解析后子命令属性非空，委托给子命令执行
            var subCommand = find_active_subcommand(command!);
            var executeTarget = subCommand ?? command!;

            var ctx = create_command_context(args);

            if (!await before_execute(ctx)) return (int)ExitCode.Cancelled;

            var exitCode = await middleware_pipeline.execute(
                ctx,
                () => executeTarget.execute(ctx, CancellationToken.None),
                CancellationToken.None);

            await after_execute(ctx, exitCode);

            return (int)exitCode;
        }
        catch (Exception ex)
        {
            if (await on_error(create_command_context(args), ex)) return (int)ExitCode.Error;

            await System.Console.Error.WriteLineAsync($"错误: {ex}");
            await System.Console.Error.WriteLineAsync(ex.StackTrace ?? "");
            return (int)ExitCode.UnhandledException;
        }
    }

    /// <summary>
    ///     从已解析的命令实例中查找被激活的子命令属性
    /// </summary>
    private static ICommand? find_active_subcommand<T>(T command) where T : ICommand
    {
        var subProps = command.GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<SubcommandAttribute>() != null);

        foreach (var prop in subProps)
        {
            if (prop.GetValue(command) is ICommand subCommand)
            {
                return subCommand;
            }
        }

        return null;
    }

    /// <summary>
    ///     从命令类型的 attribute 元数据生成帮助文本
    /// </summary>
    private static string generate_attribute_help<T>(string appName) where T : ICommand
    {
        var type = typeof(T);
        var cmdAttr = type.GetCustomAttribute<CommandAttribute>();
        var description = cmdAttr?.description ?? _app_description;

        // 收集子命令信息
        var subProps = type.GetProperties()
            .Where(p => p.GetCustomAttribute<SubcommandAttribute>() != null)
            .ToList();

        var commands = new List<CommandInfo>();
        foreach (var prop in subProps)
        {
            var subType = prop.PropertyType;
            var subAttr = subType.GetCustomAttribute<CommandAttribute>();
            if (subAttr != null)
            {
                commands.Add(new CommandInfo
                {
                    name = subAttr.name,
                    description = subAttr.description,
                    command_type = subType
                });
            }
        }

        return HelpGenerator.generate_root_help_from_command_info(
            appName, description, commands, _app_version);
    }

    /// <summary>
    ///     函数式风格：委托参数自动映射为命令行选项，通过中间件管道执行
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="handler">处理委托</param>
        /// <summary>
    ///     函数式风格：异步委托参数自动映射为命令行选项，通过中间件管道执行
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="handler">处理委托</param>
    /// <returns>退出码</returns>
    public static async Task<int> run(string[] args, Delegate handler)
    {
        try
        {
            var model = BuiltCommandModel.from_delegate("app", handler);
            return await execute_with_middleware(model, args);
        }
        catch (Exception ex)
        {
            await System.Console.Error.WriteLineAsync($"错误: {ex}");
            await System.Console.Error.WriteLineAsync(ex.StackTrace ?? "");
            return (int)ExitCode.UnhandledException;
        }
    }

    /// <summary>
    ///     多命令注册方式（兼容层）：通过 CommandRegistryBuilder 注册多个命令，通过中间件管道执行
    ///     <para>
    ///         新增 CLI 工具应使用 <c>run&lt;TRootCommand&gt;()</c> 的 attribute-first 模式。
    ///     </para>
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="configure">命令注册委托</param>
    /// <returns>退出码</returns>
    public static int run_sync(string[] args, Action<CommandRegistryBuilder> configure)
    {
        return run(args, configure).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     多命令注册方式（兼容层）：异步通过 CommandRegistryBuilder 注册多个命令，通过中间件管道执行
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="configure">命令注册委托</param>
    /// <returns>退出码</returns>
    public static async Task<int> run(string[] args, Action<CommandRegistryBuilder> configure)
    {
        try
        {
            var registry = new CommandRegistryBuilder();
            configure(registry);

            var appName = resolve_app_name();

            if (args.Length == 0)
            {
                var helpText = HelpGenerator.generate_root_help(
                    appName, _app_description, registry._commands, _app_version);
                System.Console.WriteLine(helpText);
                return (int)ExitCode.Success;
            }

            var firstArg = args[0];

            if (firstArg is "--help" or "-h")
            {
                var helpText = HelpGenerator.generate_root_help(
                    appName, _app_description, registry._commands, _app_version);
                System.Console.WriteLine(helpText);
                return (int)ExitCode.Success;
            }

            if (firstArg is "--version" or "-v")
            {
                var version = string.IsNullOrEmpty(_app_version) ? "1.0.0" : _app_version;
                System.Console.WriteLine($"{appName} v{version}");
                return (int)ExitCode.Success;
            }

            var commandName = firstArg;
            var matched = registry._commands.FirstOrDefault(c =>
                string.Equals(c.name, commandName, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                var commandArgs = args[1..];
                return await execute_with_middleware(matched, commandArgs);
            }

            await System.Console.Error.WriteLineAsync($"未知命令: {commandName}");
            await System.Console.Error.WriteLineAsync($"使用 '{appName} --help' 查看可用命令");
            return (int)ExitCode.CommandNotFound;
        }
        catch (Exception ex)
        {
            await System.Console.Error.WriteLineAsync($"错误: {ex}");
            await System.Console.Error.WriteLineAsync(ex.StackTrace ?? "");
            return (int)ExitCode.UnhandledException;
        }
    }

    /// <summary>
    ///     生成指定 Shell 的补全脚本
    /// </summary>
    /// <param name="shell">Shell 类型（bash/zsh/powershell/fish）</param>
    /// <param name="appName">应用命令名称</param>
    /// <param name="commands">命令列表</param>
    /// <returns>补全脚本文本</returns>
    public static string generate_completion_script(string shell, string appName, params CommandInfo[] commands)
    {
        return CompletionScriptGenerator.generate(shell, appName, commands);
    }

    /// <summary>
    ///     根据 CommandRegistryBuilder 生成补全脚本
    /// </summary>
    /// <param name="shell">Shell 类型</param>
    /// <param name="appName">应用命令名称</param>
    /// <param name="registry">命令注册构建器</param>
    /// <returns>补全脚本文本</returns>
    public static string generate_completion_script(string shell, string appName, CommandRegistryBuilder registry)
    {
        return CompletionScriptGenerator.generate_for_commands(shell, appName, registry._commands);
    }

    /// <summary>
    ///     获取支持的 Shell 列表
    /// </summary>
    public static IReadOnlyList<string> get_supported_shells()
    {
        return CompletionScriptGenerator.supported_shells;
    }

    private static async Task<int> execute_with_middleware(BuiltCommandModel model, string[] args)
    {
        var ctx = create_command_context(args);

        if (!await before_execute(ctx)) return (int)ExitCode.Cancelled;

        var exitCode = await middleware_pipeline.execute(
            ctx,
            () => Task.FromResult((ExitCode)CliArgumentParser.parse_and_execute(model, args)),
            CancellationToken.None
        );

        await after_execute(ctx, exitCode);

        return (int)exitCode;
    }

    private static CommandContext create_command_context(string[] args)
    {
        return new CommandContext
        {
            cancellation_token = CancellationToken.None,
            services = _services ?? CommandContext.empty_provider,
            output = ConsoleOutputWriter.instance,
            input = ConsoleInputReader.instance
        };
    }

    private static Task<bool> before_execute(CommandContext ctx)
    {
        return _lifecycle_hooks.run_before_execute(ctx);
    }

    private static Task after_execute(CommandContext ctx, ExitCode exitCode)
    {
        return _lifecycle_hooks.run_after_execute(ctx, exitCode);
    }

    private static Task<bool> on_error(ICommandContext context, Exception exception)
    {
        return _lifecycle_hooks.run_on_error(context, exception);
    }

    private static string resolve_app_name()
    {
        if (!string.IsNullOrEmpty(_app_name)) return _app_name;

        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
        }
        catch
        {
            return "app";
        }
    }
}