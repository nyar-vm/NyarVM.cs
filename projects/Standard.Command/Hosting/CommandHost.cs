using Core.Command;
using Core.Console;
using Core.Terminal;
using Std.Command.Middleware;
using Std.Command.Ports;
using Std.Console;

namespace Std.Command.Hosting;

/// <summary>
///     Command 宿主，封装命令注册表、中间件管道、I/O 端口、生命周期钩子和 Shell 生命周期管理
/// </summary>
public sealed class CommandHost
{
    private readonly List<ILifecycleHook> _lifecycle_hooks;

    /// <summary>
    ///     创建 Command 宿主
    /// </summary>
    /// <param name="registry">命令注册表</param>
    /// <param name="pipeline">中间件管道</param>
    /// <param name="services">服务提供者</param>
    /// <param name="lifecycleHooks">生命周期钩子列表</param>
    /// <param name="shell">Shell 宿主实例</param>
    /// <param name="output">输出端口</param>
    /// <param name="input">输入端口</param>
    public CommandHost(
        CommandRegistry registry,
        MiddlewarePipeline pipeline,
        IServiceProvider services,
        List<ILifecycleHook>? lifecycleHooks = null,
        IInteractiveShell? shell = null,
        IOutputWriter? output = null,
        IInputReader? input = null)
    {
        this.registry = registry;
        this.pipeline = pipeline;
        this.services = services;
        _lifecycle_hooks = lifecycleHooks ?? [];
        this.shell = shell;
        this.output = output;
        this.input = input;
    }

    /// <summary>
    ///     命令注册表
    /// </summary>
    public CommandRegistry registry { get; }

    /// <summary>
    ///     中间件管道
    /// </summary>
    public MiddlewarePipeline pipeline { get; }

    /// <summary>
    ///     依赖注入服务提供者
    /// </summary>
    public IServiceProvider services { get; }

    /// <summary>
    ///     当前 Shell 宿主实例
    /// </summary>
    public IInteractiveShell? shell { get; }

    /// <summary>
    ///     输出端口
    /// </summary>
    public IOutputWriter? output { get; }

    /// <summary>
    ///     输入端口
    /// </summary>
    public IInputReader? input { get; }

    /// <summary>
    ///     异步运行宿主，触发生命周期钩子并通过全局中间件管道包裹 Shell 执行
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>强类型退出码</returns>
    public async Task<ExitCode> run(CancellationToken cancellationToken = default)
    {
        if (shell is null) return ExitCode.Error;

        var hostContext = create_host_context(cancellationToken);

        if (!await trigger_before_execute(hostContext)) return ExitCode.Cancelled;

        try
        {
            var exitCode = await pipeline.execute(
                hostContext,
                () => shell.run(cancellationToken),
                cancellationToken
            );

            await trigger_after_execute(hostContext, exitCode);

            return exitCode;
        }
        catch (Exception ex)
        {
            if (await trigger_on_error(hostContext, ex)) return ExitCode.Error;

            throw;
        }
    }

    /// <summary>
    ///     停止宿主
    /// </summary>
    public async Task stop()
    {
        if (shell is not null) await shell.stop();
    }

    private CommandContext create_host_context(CancellationToken cancellation)
    {
        return new CommandContext
        {
            cancellation_token = cancellation,
            services = services,
            output = output ?? ConsoleOutputWriter.instance,
            input = input ?? ConsoleInputReader.instance
        };
    }

    private Task<bool> trigger_before_execute(ICommandContext context)
    {
        return _lifecycle_hooks.run_before_execute(context);
    }

    private Task trigger_after_execute(ICommandContext context, ExitCode exitCode)
    {
        return _lifecycle_hooks.run_after_execute(context, exitCode);
    }

    private Task<bool> trigger_on_error(ICommandContext context, Exception exception)
    {
        return _lifecycle_hooks.run_on_error(context, exception);
    }
}