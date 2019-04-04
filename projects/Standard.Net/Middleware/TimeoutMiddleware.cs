using System.Text;
using Std.Net.Http;

namespace Std.Net.Middleware;

/// <summary>
///     请求超时中间件——在指定时间内未完成则取消请求并返回 504 Gateway Timeout。
/// </summary>
public sealed class TimeoutMiddleware : IMiddleware
{
    private readonly TimeSpan _timeout;

    /// <summary>
    ///     初始化请求超时中间件。
    /// </summary>
    /// <param name="timeout">请求超时时间（默认 30 秒）</param>
    public TimeoutMiddleware(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public async ValueTask invoke(RouteContext context, Func<ValueTask> next)
    {
        using var cts = new CancellationTokenSource(_timeout);

        try
        {
            await next();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            context.response.headers.TryAdd("Content-Type", "application/json");

            context.response.status_code = (HttpStatusCode)504;
            context.response.body = Encoding.UTF8.GetBytes(
                """{"error":"请求超时（Gateway Timeout）","code":504}""");
        }
    }
}