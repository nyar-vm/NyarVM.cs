using Core.Terminal;
using Std.Command.Middleware;
using Std.Command.Ports;
using Std.Console;
using ICommand = Core.Command.ICommand;

namespace Std.Command;

/// <summary>
///     CLI 应用程序宿主，实现 IInteractiveShell，负责一次性命令行调度
///     解析 args → 匹配命令 → 通过管道执行 → 返回退出码
/// </summary>
public sealed class CliApplication : IInteractiveShell
{
    private readonly string[] _args;
    private readonly MiddlewarePipeline _pipeline;
    private readonly CommandRegistry _registry;
    private ShellState _state = ShellState.idle;

    /// <summary>
    ///     创建 CLI 应用宿主
    /// </summary>
    /// <param name="name">应用名称</param>
    /// <param name="registry">命令注册表</param>
    /// <param name="pipeline">中间件管道</param>
    public CliApplication(string name, CommandRegistry registry, MiddlewarePipeline pipeline)
    {
        this.name = name;
        _registry = registry;
        _pipeline = pipeline;

        var rawArgs = Environment.GetCommandLineArgs();

        if (rawArgs.Length > 1)
            _args = rawArgs[1..];
        else
            _args = [];
    }

    /// <inheritdoc />
    public string name { get; }

    /// <inheritdoc />
    public ShellState state
    {
        get => _state;
        private set
        {
            if (_state == value) return;

            var oldState = _state;
            _state = value;
            StateChanged?.Invoke(oldState, value);
        }
    }

    /// <inheritdoc />
    public event Action<ShellState, ShellState>? StateChanged;

    /// <inheritdoc />
    public async Task<ExitCode> run(CancellationToken cancellation = default)
    {
        state = ShellState.starting;
        state = ShellState.running;

        try
        {
            if (_args.Length == 0) return ExitCode.Success;

            var commandName = _args[0];
            var commandType = _registry.resolve(commandName);

            if (commandType is null)
            {
                ConsoleOutputWriter.instance.write_error($"未知命令: {commandName}");
                return ExitCode.CommandNotFound;
            }

            var context = new CommandContext
            {
                cancellation_token = cancellation,
                output = ConsoleOutputWriter.instance,
                input = ConsoleInputReader.instance
            };

            var command = (ICommand)Activator.CreateInstance(commandType)!;

            return await _pipeline.execute(context, () => command.execute(context, cancellation), cancellation);
        }
        catch (OperationCanceledException)
        {
            return ExitCode.Cancelled;
        }
        catch (Exception ex)
        {
            ConsoleOutputWriter.instance.write_error($"未处理的异常: {ex.Message}");
            return ExitCode.UnhandledException;
        }
        finally
        {
            state = ShellState.stopping;
            state = ShellState.stopped;
        }
    }

    /// <inheritdoc />
    public Task stop()
    {
        state = ShellState.stopping;
        state = ShellState.stopped;
        return Task.CompletedTask;
    }

    private static Dictionary<string, object?> build_arguments(string[] positionalArgs)
    {
        var args = new Dictionary<string, object?>();

        for (var i = 0; i < positionalArgs.Length; i++) args[$"arg{i}"] = positionalArgs[i];

        return args;
    }
}