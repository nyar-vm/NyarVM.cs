using Std.Result;

namespace Std.App.Server.Core;

/// <summary>
///     Atlas 控制器基类，提供 InvokeAsync 方法自动将 Result&lt;T&gt; 转换为 AtlasResult。
///     控制器作为入站适配器，不包含业务逻辑，只做协议转换。
/// </summary>
public abstract class AtlasController : IController
{
    /// <summary>
    ///     调用异步业务方法并自动转换 Result&lt;T&gt; 为 AtlasResult
    /// </summary>
    /// <typeparam name="T">业务返回值类型</typeparam>
    /// <param name="action">异步业务方法</param>
    /// <returns>AtlasResult</returns>
    protected AtlasResult InvokeAsync<T>(Func<Task<Result<T>>> action)
    {
        try
        {
            var result = action().GetAwaiter().GetResult();

            if (result.is_success) return AtlasResult.ok(result.value);

            return AtlasResult.bad_request(result.error ?? "操作失败");
        }
        catch (UnauthorizedAccessException)
        {
            return AtlasResult.forbidden();
        }
        catch (Exception ex)
        {
            return AtlasResult.internal_error(ex.Message);
        }
    }

    /// <summary>
    ///     调用异步业务方法并自动转换非泛型 Result 为 AtlasResult
    /// </summary>
    /// <param name="action">异步业务方法</param>
    /// <returns>AtlasResult</returns>
    protected AtlasResult InvokeAsync(Func<Task<Result.Result>> action)
    {
        try
        {
            var result = action().GetAwaiter().GetResult();

            if (result.is_success) return AtlasResult.ok(new { success = true });

            return AtlasResult.bad_request(result.error ?? "操作失败");
        }
        catch (UnauthorizedAccessException)
        {
            return AtlasResult.forbidden();
        }
        catch (Exception ex)
        {
            return AtlasResult.internal_error(ex.Message);
        }
    }
}