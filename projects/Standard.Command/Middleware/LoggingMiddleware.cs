using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     日志中间件，记录命令名称、参数和执行时间
/// </summary>
public sealed class LoggingMiddleware : ICommandMiddleware
{
    /// <summary>
    ///     日志输出目标
    /// </summary>
    public TextWriter log_output { get; init; } = System.Console.Error;

    /// <summary>
    ///     是否详细输出参数信息
    /// </summary>
    public bool verbose { get; init; }

    /// <inheritdoc />
    public async Task<ExitCode> invoke(ICommandContext context, Func<Task<ExitCode>> next,
        CancellationToken cancellationToken)
    {
        var commandName = context.GetType().Name;
        await log_output.WriteLineAsync($"[Iris] 开始执行命令: {commandName}");

        if (verbose && context.arguments.Count > 0)
            foreach (var (key, value) in context.arguments)
                await log_output.WriteLineAsync($"  --{key}: {value}");

        var exitCode = await next();

        await log_output.WriteLineAsync($"[Iris] 命令执行完成，退出码: {(int)exitCode}");

        return exitCode;
    }
}