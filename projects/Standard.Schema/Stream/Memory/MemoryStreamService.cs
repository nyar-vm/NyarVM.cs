using System.Collections.Concurrent;

namespace Hermes.Stream.Memory;

public sealed class MemoryStreamService : IStreamService
{
    private readonly StreamOptions _options;
    private readonly ConcurrentDictionary<string, ConcurrentBag<Delegate>> _subscribers = new();

    public MemoryStreamService(StreamOptions options)
    {
        _options = options;
    }

    public Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default)
    {
        var fullTopic = BuildTopic(topic);

        if (_subscribers.TryGetValue(fullTopic, out var handlers))
            foreach (var handler in handlers)
                if (handler is Action<T> typedHandler)
                    typedHandler(message);

        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string topic, Action<T> handler, CancellationToken ct = default)
    {
        var fullTopic = BuildTopic(topic);
        var bag = _subscribers.GetOrAdd(fullTopic, _ => []);
        bag.Add(handler);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<T>(string topic, Action<T> handler, CancellationToken ct = default)
    {
        var fullTopic = BuildTopic(topic);

        if (_subscribers.TryGetValue(fullTopic, out var bag))
        {
            var remaining = new ConcurrentBag<Delegate>(bag.Where(h => h != (Delegate)handler));
            _subscribers.TryUpdate(fullTopic, remaining, bag);
        }

        return Task.CompletedTask;
    }

    private string BuildTopic(string topic)
    {
        return $"{_options.TopicPrefix}{topic}";
    }
}