namespace Std.App.Server.Systems;

/// <summary>
///     内存事件总线实现，基于 IEventHandler 订阅。
///     事件在发布时同步调用所有已注册的处理器。
/// </summary>
public sealed class InMemoryAtlasEventBus : IAtlasEventBus
{
    private readonly Dictionary<Type, List<object>> _handlers = new();

    /// <inheritdoc />
    public Task PublishAsync<T>(T @event) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            var tasks = list.Select(h => ((IEventHandler<T>)h).HandleAsync(@event)).ToArray();
            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    public void Register<T>(IEventHandler<T> handler) where T : class
    {
        var eventType = typeof(T);
        if (!_handlers.TryGetValue(eventType, out var list))
        {
            list = [];
            _handlers[eventType] = list;
        }

        list.Add(handler);
    }
}