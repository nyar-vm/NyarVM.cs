using Core.State;

namespace Std.State;

/// <summary>
///     可写状态，实现 IWritableState&lt;T&gt; 接口，提供状态值的读写
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public sealed class WritableState<T> : IWritableState<T>
{
    /// <summary>
    ///     状态值
    /// </summary>
    private T _value;

    /// <summary>
    ///     初始化可写状态
    /// </summary>
    /// <param name="initialValue">初始值</param>
    public WritableState(T initialValue)
    {
        _value = initialValue;
    }

    /// <summary>
    ///     获取或设置状态值
    /// </summary>
    public T value
    {
        get => _value;
        set => _value = value;
    }
}