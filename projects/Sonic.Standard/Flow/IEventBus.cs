namespace Std.Flow;

/// <summary>
///     事件总线接口，提供发布/订阅消息传递能力。
///     扩展了 Sonic.Core 的 <see cref="Core.Flow.Message.IEventBus" /> 接口，
///     增加了带取消令牌的发布、带订阅令牌的取消订阅等能力。
/// </summary>
public interface IEventBus : Core.Flow.Message.IEventBus
{
    /// <summary>
    ///     异步发布事件，支持取消令牌。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步操作</returns>
    Task publish<T>(T @event, CancellationToken cancellationToken = default);

    /// <summary>
    ///     订阅指定类型的事件，返回订阅令牌用于取消订阅。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    SubscriptionToken subscribe<T>(Func<T, CancellationToken, Task> handler);

    /// <summary>
    ///     取消订阅。
    /// </summary>
    /// <param name="token">订阅令牌</param>
    void unsubscribe(SubscriptionToken token);
}

/// <summary>
///     订阅令牌，用于标识和管理订阅关系。
/// </summary>
public sealed class SubscriptionToken : IEquatable<SubscriptionToken>
{
    private readonly Guid _id = Guid.NewGuid();

    /// <inheritdoc />
    public bool Equals(SubscriptionToken? other)
    {
        return other is not null && _id == other._id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SubscriptionToken other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}