using System.Collections.Concurrent;

namespace Std.Flow;

/// <summary>
///     内存事件总线实现，在进程内完成事件的发布和订阅分发。
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Subscription>> _subscribers = new();

    /// <inheritdoc />
    public async Task publish<T>(T @event, CancellationToken cancellationToken = default)
    {
        var concreteType = typeof(T);

        if (!_subscribers.TryGetValue(concreteType, out var subscriptions)) return;

        foreach (var subscription in subscriptions.ToList())
            if (subscription.handler is Func<T, CancellationToken, Task> typedHandler)
                await typedHandler(@event, cancellationToken);
    }

    /// <inheritdoc />
    Task Core.Flow.Message.IEventBus.publish<T>(T @event)
    {
        return publish(@event, CancellationToken.None);
    }

    /// <inheritdoc />
    IDisposable Core.Flow.Message.IEventBus.subscribe<T>(Func<T, Task> handler)
    {
        var token = subscribe<T>((evt, _) => handler(evt));
        return new SubscriptionDisposable(this, token);
    }

    /// <inheritdoc />
    public SubscriptionToken subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        var token = new SubscriptionToken();
        var subscription = new Subscription(token, handler);
        var eventType = typeof(T);

        _subscribers.AddOrUpdate(
            eventType,
            _ => [subscription],
            (_, list) =>
            {
                list.Add(subscription);
                return list;
            });

        return token;
    }

    /// <inheritdoc />
    public void unsubscribe(SubscriptionToken token)
    {
        foreach (var (_, subscriptions) in _subscribers) subscriptions.RemoveAll(s => s.token.Equals(token));
    }

    /// <summary>
    ///     获取指定事件类型的订阅者数量。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>订阅者数量</returns>
    public int get_subscriber_count<T>()
    {
        if (_subscribers.TryGetValue(typeof(T), out var subscriptions)) return subscriptions.Count;

        return 0;
    }

    private sealed class Subscription
    {
        public Subscription(SubscriptionToken token, object handler)
        {
            this.token = token;
            this.handler = handler;
        }

        public SubscriptionToken token { get; }
        public object handler { get; }
    }

    private sealed class SubscriptionDisposable(IEventBus bus, SubscriptionToken token) : IDisposable
    {
        public void Dispose()
        {
            bus.unsubscribe(token);
        }
    }
}