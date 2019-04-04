namespace Core.Compiler.Assertion;

/// <summary>
///     泛型断言接口，定义类型相关的比较断言契约
/// </summary>
/// <typeparam name="T">可比较的类型</typeparam>
public interface IAssert<T>
{
    /// <summary>
    ///     断言值在指定范围内
    /// </summary>
    /// <param name="value">待检查的值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="message">失败消息</param>
    void is_in_range(T value, T min, T max, string? message = null);

    /// <summary>
    ///     断言值大于另一个值
    /// </summary>
    /// <param name="value">待检查的值</param>
    /// <param name="other">比较目标值</param>
    /// <param name="message">失败消息</param>
    void is_greater_than(T value, T other, string? message = null);
}