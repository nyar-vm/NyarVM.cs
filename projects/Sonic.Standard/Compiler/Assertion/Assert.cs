using Core.Compiler.Assertion;

namespace Std.Compiler.Assertion;

/// <summary>
///     断言工具类，实现 <see cref="IAssert" /> 接口，提供通用断言操作。
/// </summary>
public sealed class Assert : IAssert
{
    /// <summary>
    ///     断言条件为真。
    /// </summary>
    /// <param name="condition">条件值。</param>
    /// <param name="message">失败消息。</param>
    public void is_true(bool condition, string? message = null)
    {
        if (!condition) throw new AssertionException(message ?? "断言失败：期望条件为真");
    }

    /// <summary>
    ///     断言条件为假。
    /// </summary>
    /// <param name="condition">条件值。</param>
    /// <param name="message">失败消息。</param>
    public void is_false(bool condition, string? message = null)
    {
        if (condition) throw new AssertionException(message ?? "断言失败：期望条件为假");
    }

    /// <summary>
    ///     断言两个值相等。
    /// </summary>
    /// <param name="expected">期望值。</param>
    /// <param name="actual">实际值。</param>
    /// <param name="message">失败消息。</param>
    public void are_equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(message ?? $"断言失败：期望 {expected}，实际 {actual}");
    }

    /// <summary>
    ///     断言两个值不相等。
    /// </summary>
    /// <param name="expected">期望值。</param>
    /// <param name="actual">实际值。</param>
    /// <param name="message">失败消息。</param>
    public void are_not_equal<T>(T expected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertionException(message ?? $"断言失败：值不应相等 {expected}");
    }

    /// <summary>
    ///     断言值为空。
    /// </summary>
    /// <param name="value">待检查的值。</param>
    /// <param name="message">失败消息。</param>
    public void is_null(object? value, string? message = null)
    {
        if (value is not null) throw new AssertionException(message ?? "断言失败：期望值为空");
    }

    /// <summary>
    ///     断言值不为空。
    /// </summary>
    /// <param name="value">待检查的值。</param>
    /// <param name="message">失败消息。</param>
    public void is_not_null(object? value, string? message = null)
    {
        if (value is null) throw new AssertionException(message ?? "断言失败：期望值不为空");
    }
}

/// <summary>
///     断言失败异常，当断言条件不满足时抛出。
/// </summary>
public sealed class AssertionException : Exception
{
    /// <summary>
    ///     初始化 <see cref="AssertionException" /> 的新实例。
    /// </summary>
    /// <param name="message">异常消息。</param>
    public AssertionException(string message) : base(message)
    {
    }

    /// <summary>
    ///     初始化 <see cref="AssertionException" /> 的新实例。
    /// </summary>
    /// <param name="message">异常消息。</param>
    /// <param name="innerException">内部异常。</param>
    public AssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}