namespace Core.Compiler.Assertion;

/// <summary>
///     断言接口，定义通用断言操作的契约
/// </summary>
public interface IAssert
{
    /// <summary>
    ///     断言条件为真
    /// </summary>
    /// <param name="condition">条件值</param>
    /// <param name="message">失败消息</param>
    void is_true(bool condition, string? message = null);

    /// <summary>
    ///     断言条件为假
    /// </summary>
    /// <param name="condition">条件值</param>
    /// <param name="message">失败消息</param>
    void is_false(bool condition, string? message = null);

    /// <summary>
    ///     断言两个值相等
    /// </summary>
    /// <param name="expected">期望值</param>
    /// <param name="actual">实际值</param>
    /// <param name="message">失败消息</param>
    void are_equal<T>(T expected, T actual, string? message = null);

    /// <summary>
    ///     断言两个值不相等
    /// </summary>
    /// <param name="expected">期望值</param>
    /// <param name="actual">实际值</param>
    /// <param name="message">失败消息</param>
    void are_not_equal<T>(T expected, T actual, string? message = null);

    /// <summary>
    ///     断言值为空
    /// </summary>
    /// <param name="value">待检查的值</param>
    /// <param name="message">失败消息</param>
    void is_null(object? value, string? message = null);

    /// <summary>
    ///     断言值不为空
    /// </summary>
    /// <param name="value">待检查的值</param>
    /// <param name="message">失败消息</param>
    void is_not_null(object? value, string? message = null);
}