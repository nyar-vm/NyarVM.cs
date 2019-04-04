using Core.Command;
using Core.Terminal;
using ICommand = Core.Command.ICommand;

namespace Std.Command;

/// <summary>
///     命令基类，提供默认 Name/Description 实现
/// </summary>
public abstract class BaseCommand : ICommand
{
    /// <inheritdoc />
    public virtual string name => GetType().Name;

    /// <inheritdoc />
    public virtual string? description => null;

    /// <inheritdoc />
    public abstract Task<ExitCode> execute(ICommandContext context, CancellationToken cancel);
}