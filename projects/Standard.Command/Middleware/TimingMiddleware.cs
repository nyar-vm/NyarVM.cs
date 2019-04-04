using System.Diagnostics;
using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     计时中间件，测量命令执行耗时
/// </summary>
public sealed class TimingMiddleware : ICommandMiddleware
{
    /// <summary>
    ///     计时输出目标
    /// </summary>
    public TextWriter timing_output { get; init; } = System.Console.Error;

    /// <summary>
    ///     耗时超过此阈值时输出警告，单位毫秒。0 表示始终输出
    /// </summary>
    public long warning_threshold_ms { get; init; }

    /// <inheritdoc />
    public async Task<ExitCode> invoke(ICommandContext context, Func<Task<ExitCode>> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var exitCode = await next();

        sw.Stop();

        if (warning_threshold_ms == 0 || sw.ElapsedMilliseconds > warning_threshold_ms)
        {
            var level = sw.ElapsedMilliseconds > (warning_threshold_ms > 0 ? warning_threshold_ms : 5000)
                ? "警告"
                : "信息";

            await timing_output.WriteLineAsync($"[Iris·计时|{level}] 命令执行耗时: {sw.ElapsedMilliseconds}ms");
        }

        return exitCode;
    }
}