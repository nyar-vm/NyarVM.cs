namespace Std.App.Client;

/// <summary>
///     响应式状态的默认实现，变更时自动通知所有订阅者
/// </summary>
/// <typeparam name="T">状态值类型</typeparam>
public sealed class ReactiveState<T> : IState<T>, IDisposable
{
    private readonly List<Action<T>> _listeners = [];
    private T _value;

    /// <summary>
    ///     初始化状态容器
    /// </summary>
    /// <param name="initialValue">初始值</param>
    public ReactiveState(T initialValue)
    {
        _value = initialValue;
    }

    /// <inheritdoc />
    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;

            _value = value;
            NotifyListeners();
        }
    }

    /// <inheritdoc />
    public event Action<T>? OnChanged;

    /// <inheritdoc />
    public IDisposable Subscribe(Action<T> listener)
    {
        _listeners.Add(listener);
        return new UnsubscribeToken(() => _listeners.Remove(listener));
    }

    private void NotifyListeners()
    {
        OnChanged?.Invoke(_value);

        foreach (var listener in _listeners) listener(_value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _listeners.Clear();
        OnChanged = null;
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