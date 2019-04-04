namespace Std.App.Client;

/// <summary>
///     响应式状态容器接口，变更时自动通知订阅者
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public interface IState<T>
{
    /// <summary>
    ///     当前状态值
    /// </summary>
    T Value { get; set; }

    /// <summary>
    ///     状态变更事件
    /// </summary>
    event Action<T>? OnChanged;

    /// <summary>
    ///     订阅状态变更
    /// </summary>
    /// <param name="listener">变更监听器</param>
    /// <returns>取消订阅的句柄</returns>
    IDisposable Subscribe(Action<T> listener);
}