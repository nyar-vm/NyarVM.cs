namespace Std.Collection;

/// <summary>
///     栈接�?///
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IStack<T> : IPushable<T>, IPopable<T>
{
    /// <summary>
    ///     压栈
    /// </summary>
    /// <param name="value">要压入的元素</param>
    new void push(T value);

    /// <summary>
    ///     弹栈
    /// </summary>
    T pop();

    /// <summary>
    ///     查看栈顶元素但不移除
    /// </summary>
    T peek();
}