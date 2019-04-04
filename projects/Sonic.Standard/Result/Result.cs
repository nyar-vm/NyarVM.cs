namespace Std.Result;

/// <summary>
///     业务统一返回类型，封装成功与失败信息。
///     系统方法返回 Result&lt;T&gt;.Success(data) 或 Result&lt;T&gt;.Failure(error, code)，
///     由 AtlasController.InvokeAsync 自动转换为 HTTP 响应。
/// </summary>
/// <typeparam name="T">成功时的值类型</typeparam>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, string? code)
    {
        is_success = isSuccess;
        this.value = value;
        this.error = error;
        this.code = code;
    }

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool is_success { get; }

    /// <summary>
    ///     成功时的值，失败时为默认值
    /// </summary>
    public T? value { get; }

    /// <summary>
    ///     失败时的错误消息
    /// </summary>
    public string? error { get; }

    /// <summary>
    ///     失败时的错误代码
    /// </summary>
    public string? code { get; }

    /// <summary>
    ///     创建成功结果
    /// </summary>
    /// <param name="value">成功值</param>
    /// <returns>成功结果</returns>
    public static Result<T> success(T value)
    {
        return new Result<T>(true, value, null, null);
    }

    /// <summary>
    ///     创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    /// <param name="code">错误代码</param>
    /// <returns>失败结果</returns>
    public static Result<T> failure(string error, string? code = null)
    {
        return new Result<T>(false, default, error, code);
    }
}

/// <summary>
///     非泛型业务返回类型，用于无返回值的操作。
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, string? error, string? code)
    {
        is_success = isSuccess;
        this.error = error;
        this.code = code;
    }

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool is_success { get; }

    /// <summary>
    ///     失败时的错误消息
    /// </summary>
    public string? error { get; }

    /// <summary>
    ///     失败时的错误代码
    /// </summary>
    public string? code { get; }

    /// <summary>
    ///     创建成功结果
    /// </summary>
    /// <returns>成功结果</returns>
    public static Result success()
    {
        return new Result(true, null, null);
    }

    /// <summary>
    ///     创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    /// <param name="code">错误代码</param>
    /// <returns>失败结果</returns>
    public static Result failure(string error, string? code = null)
    {
        return new Result(false, error, code);
    }
}