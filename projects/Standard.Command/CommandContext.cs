using Core.Command;
using Core.Console;
using Core.Terminal;
using Std.Command.Ports;

namespace Std.Command;

public sealed class CommandContext : ICommandContext
{
    /// <summary>
    ///     空服务提供者的静态实例
    /// </summary>
    public static readonly IServiceProvider empty_provider = new SystemServiceProvider();

    private readonly Dictionary<string, object?> _arguments = new();

    /// <summary>
    ///     当前交互式 Shell 实例
    /// </summary>
    public IInteractiveShell? shell { get; set; }

    public IReadOnlyDictionary<string, object?> arguments => _arguments;

    public IServiceProvider services { get; set; } = empty_provider;

    public IConsole? console { get; set; }

    public IOutputWriter? output { get; set; }

    public IInputReader? input { get; set; }

    public ITerminalRenderer? renderer { get; set; }

    public ILocalizer? localizer { get; set; }

    public CancellationToken cancellation_token { get; set; }

    public T? get_option_value<T>(string name)
    {
        if (_arguments.TryGetValue(name, out var value) && value is T typed) return typed;

        return default;
    }

    public void set_argument(string name, object? value)
    {
        _arguments[name] = value;
    }

    internal sealed class SystemServiceProvider : IServiceProvider
    {
        public static readonly SystemServiceProvider empty = new();

        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}