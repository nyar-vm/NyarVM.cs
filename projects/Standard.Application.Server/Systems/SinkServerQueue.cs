namespace Std.App.Server.Systems;

/// <summary>
///     空队列实现，所有入队操作为空操作。
///     用于不需要队列功能的场景或测试。
/// </summary>
public sealed class SinkServerQueue : IAtlasQueue
{
    /// <inheritdoc />
    public Task EnqueueAsync<T>(string queueName, T payload, int? priority = null, TimeSpan? delay = null)
    {
        return Task.CompletedTask;
    }
}