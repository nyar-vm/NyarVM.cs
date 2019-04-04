namespace Std.App.Client;

/// <summary>
///     全局状态树接口，类似 Redux / Zustand 模式
/// </summary>
public interface IStore
{
    /// <summary>
    ///     获取指定键的状态切片
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <param name="key">状态键</param>
    T? GetState<T>(string key);

    /// <summary>
    ///     设置指定键的状态切片，并通知订阅者
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <param name="key">状态键</param>
    /// <param name="value">新值</param>
    void SetState<T>(string key, T value);

    /// <summary>
    ///     订阅指定键的状态变更
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    /// <param name="key">状态键</param>
    /// <param name="listener">变更监听器</param>
    /// <returns>取消订阅的句柄</returns>
    IDisposable Subscribe<T>(string key, Action<T> listener);

    /// <summary>
    ///     重置整个状态树
    /// </summary>
    void Reset();
}