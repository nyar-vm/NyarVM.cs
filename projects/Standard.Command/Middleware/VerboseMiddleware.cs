using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     Verbose 输出中间件，根据上下文中的 Verbose 标志控制详细输出
/// </summary>
public sealed class VerboseMiddleware : ICommandMiddleware
{
    /// <summary>
    ///     详细输出目标
    /// </summary>
    public TextWriter verbose_output { get; init; } = System.Console.Out;

    /// <summary>
    ///     Verbose 模式下的输出前缀
    /// </summary>
    public string prefix { get; init; } = "  → ";

    /// <inheritdoc />
    public async Task<ExitCode> invoke(ICommandContext context, Func<Task<ExitCode>> next,
        CancellationToken cancellationToken)
    {
        var isVerbose = context.get_option_value<bool>("verbose") || context.get_option_value<bool>("v");

        if (isVerbose)
        {
            await verbose_output.WriteLineAsync($"{prefix}Verbose 模式已启用");
            await verbose_output.WriteLineAsync(
                $"{prefix}参数: {string.Join(", ", context.arguments.Select(kv => $"{kv.Key}={kv.Value}"))}");
        }

        var exitCode = await next();

        if (isVerbose) await verbose_output.WriteLineAsync($"{prefix}命令执行结束，退出码: {(int)exitCode}");

        return exitCode;
    }
}