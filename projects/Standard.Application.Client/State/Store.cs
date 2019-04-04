namespace Std.App.Client;

/// <summary>
///     全局状态树的默认实现，基于内存字典存储
/// </summary>
public sealed class Store : IStore
{
    private readonly Dictionary<string, object> _listeners = new();
    private readonly Dictionary<string, object> _states = new();

    /// <inheritdoc />
    public T? GetState<T>(string key)
    {
        if (_states.TryGetValue(key, out var value) && value is T typedValue) return typedValue;

        return default;
    }

    /// <inheritdoc />
    public void SetState<T>(string key, T value)
    {
        _states[key] = value!;

        if (_listeners.TryGetValue(key, out var listenerObj) &&
            listenerObj is List<Action<T>> typedListeners)
            foreach (var listener in typedListeners)
                listener(value);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(string key, Action<T> listener)
    {
        if (!_listeners.TryGetValue(key, out var listenerObj))
        {
            listenerObj = new List<Action<T>>();
            _listeners[key] = listenerObj;
        }

        var typedListeners = (List<Action<T>>)listenerObj;
        typedListeners.Add(listener);

        return new UnsubscribeToken(() => typedListeners.Remove(listener));
    }

    /// <inheritdoc />
    public void Reset()
    {
        _states.Clear();
        _listeners.Clear();
    }

    /// <summary>
    ///     取消订阅令牌
    /// </summary>
    private sealed class UnsubscribeToken : IDisposable
    {
        private readonly Action _action;

        public UnsubscribeToken(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}