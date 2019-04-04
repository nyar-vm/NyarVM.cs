using Core.Command;
using Core.Terminal;

namespace Std.Command.Middleware;

/// <summary>
///     异常处理中间件，捕获未处理异常并返回标准退出码
/// </summary>
public sealed class ErrorHandlingMiddleware : ICommandMiddleware
{
    /// <summary>
    ///     错误输出目标
    /// </summary>
    public TextWriter error_output { get; init; } = System.Console.Error;

    /// <summary>
    ///     是否输出完整堆栈跟踪
    /// </summary>
    public bool show_stack_trace { get; init; } = true;

    /// <inheritdoc />
    public async Task<ExitCode> invoke(ICommandContext context, Func<Task<ExitCode>> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            await error_output.WriteLineAsync("操作已取消");
            return ExitCode.Cancelled;
        }
        catch (IOException ex)
        {
            await error_output.WriteLineAsync($"文件或 I/O 错误: {ex.Message}");
            return ExitCode.FileNotFound;
        }
        catch (UnauthorizedAccessException ex)
        {
            await error_output.WriteLineAsync($"权限不足: {ex.Message}");
            return ExitCode.PermissionDenied;
        }
        catch (Exception ex)
        {
            await error_output.WriteLineAsync($"未处理异常: {ex.Message}");

            if (show_stack_trace) await error_output.WriteLineAsync(ex.StackTrace);

            return ExitCode.UnhandledException;
        }
    }
}