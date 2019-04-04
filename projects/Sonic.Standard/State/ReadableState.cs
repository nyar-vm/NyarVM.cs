using Core.State;

namespace Std.State;

/// <summary>
///     只读状态，实现 IReadableState&lt;T&gt; 接口，提供状态值的读取
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public sealed class ReadableState<T> : IReadableState<T>
{
    /// <summary>
    ///     状态值
    /// </summary>
    private readonly Func<T> _getter;

    /// <summary>
    ///     初始化只读状态
    /// </summary>
    /// <param name="getter">值获取函数</param>
    public ReadableState(Func<T> getter)
    {
        _getter = getter;
    }

    /// <summary>
    ///     获取状态值
    /// </summary>
    public T value => _getter();
}