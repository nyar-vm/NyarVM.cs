using Core.State;

namespace Std.State;

/// <summary>
///     状态存储，实现 IStateStore 接口，管理键值对状态的读写
/// </summary>
public sealed class StateStore : IStateStore
{
    /// <summary>
    ///     状态字典
    /// </summary>
    private readonly Dictionary<string, object> _states = new();

    /// <summary>
    ///     获取指定键的只读状态
    /// </summary>
    /// <typeparam name="T">状态值类型</typeparam>
    /// <param name="key">状态键</param>
    /// <returns>只读状态</returns>
    public IReadableState<T> get_state<T>(string key)
    {
        if (_states.TryGetValue(key, out var value) && value is T typedValue)
            return new ReadableState<T>(() => typedValue);
        return new ReadableState<T>(() => default!);
    }

    /// <summary>
    ///     获取指定键的可写状态
    /// </summary>
    /// <typeparam name="T">状态值类型</typeparam>
    /// <param name="key">状态键</param>
    /// <returns>可写状态</returns>
    public IWritableState<T> get_writable_state<T>(string key)
    {
        if (!_states.TryGetValue(key, out var value) || value is not T) _states[key] = default(T)!;

        var state = new WritableState<T>((T)_states[key]);
        return state;
    }
}