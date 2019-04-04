using Sonic.Terminal;
using Sonic.Command;
using Sonic.Command.Ports;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// REPL 应用程序宿主，实现 IInteractiveShell，管理 Read-Eval-Print 循环
/// </summary>
public sealed class ReplApplication : IInteractiveShell
{
    private readonly ReplEngine _engine;
    private ShellState _state = ShellState.idle;

    /// <inheritdoc />
    public string name { get; }

    /// <inheritdoc />
    public ShellState state
    {
        get => _state;
        private set
        {
            if (_state == value)
            {
                return;
            }

            var oldState = _state;
            _state = value;
            StateChanged?.Invoke(oldState, value);
        }
    }

    /// <inheritdoc />
    public event Action<ShellState, ShellState>? StateChanged;

    /// <summary>
    /// 创建 REPL 应用宿主
    /// </summary>
    /// <param name="name">REPL 名称</param>
    /// <param name="engine">REPL 引擎</param>
    public ReplApplication(string name, ReplEngine engine)
    {
        this.name = name;
        _engine = engine;
    }

    /// <inheritdoc />
    public async Task<ExitCode> run(CancellationToken cancellation = default)
    {
        state = ShellState.starting;
        state = ShellState.running;

        try
        {
            await _engine.run_loop(cancellation);
            return ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            return ExitCode.Cancelled;
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
}
