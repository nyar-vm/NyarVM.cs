using Std.App.Server.Queue;

namespace Std.App.Server.Systems;

/// <summary>
///     IAtlasQueue 适配器，委托给已有的 IQueueProvider 实现。
/// </summary>
public sealed class AtlasQueueAdapter : IAtlasQueue
{
    private readonly IQueueProvider _queueProvider;

    /// <summary>
    ///     初始化队列适配器
    /// </summary>
    /// <param name="queueProvider">队列提供者</param>
    public AtlasQueueAdapter(IQueueProvider queueProvider)
    {
        _queueProvider = queueProvider;
    }

    /// <inheritdoc />
    public Task EnqueueAsync<T>(string queueName, T payload, int? priority = null, TimeSpan? delay = null)
    {
        return _queueProvider.enqueue(queueName, payload!, priority, delay);
    }
}