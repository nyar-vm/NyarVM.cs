namespace Hermes.YYDB.Query;

public sealed class StreamQuery<T> where T : class
{
    private readonly IStreamBackend _backend;
    private string? _key;
    private T? _message;
    private string? _topic;
    private Func<T, string>? _topicSelector;

    public StreamQuery(IStreamBackend backend)
    {
        _backend = backend;
    }

    #region 消费者组

    public async Task StartConsumerAsync(string groupId, Action<T> handler)
    {
        if (_topic == null) throw new InvalidOperationException("消费必须指定主题");

        await _backend.StartConsumerAsync(_topic, groupId, handler);
    }

    #endregion

    private string ResolveTopic(T message)
    {
        if (_topic != null) return _topic;

        if (_topicSelector != null) return _topicSelector(message);

        return typeof(T).Name;
    }

    #region 主题选择

    public StreamQuery<T> Topic(string topic)
    {
        _topic = topic;
        return this;
    }

    public StreamQuery<T> Topic(Func<T, string> topicSelector)
    {
        _topicSelector = topicSelector;
        return this;
    }

    #endregion

    #region 发布

    public StreamQuery<T> Message(T message)
    {
        _message = message;
        return this;
    }

    public StreamQuery<T> MessageKey(string key)
    {
        _key = key;
        return this;
    }

    public async Task PublishAsync()
    {
        if (_message == null) throw new InvalidOperationException("必须指定消息内容");

        var topic = ResolveTopic(_message);
        await _backend.PublishAsync(topic, _message, _key);
    }

    public async Task PublishAsync(T message, string? key = null)
    {
        var topic = ResolveTopic(message);
        await _backend.PublishAsync(topic, message, key);
    }

    #endregion

    #region 订阅

    public async Task<IDisposable> SubscribeAsync(Action<T> handler)
    {
        if (_topic == null) throw new InvalidOperationException("订阅必须指定主题");

        return await _backend.SubscribeAsync(_topic, handler);
    }

    public async Task<IDisposable> SubscribeAsync(Func<T, Task> handler)
    {
        if (_topic == null) throw new InvalidOperationException("订阅必须指定主题");

        return await _backend.SubscribeAsync(_topic, handler);
    }

    #endregion
}

public interface IStreamBackend
{
    Task PublishAsync<T>(string topic, T message, string? key = null);
    Task<IDisposable> SubscribeAsync<T>(string topic, Action<T> handler);
    Task<IDisposable> SubscribeAsync<T>(string topic, Func<T, Task> handler);
    Task StartConsumerAsync<T>(string topic, string groupId, Action<T> handler);
}