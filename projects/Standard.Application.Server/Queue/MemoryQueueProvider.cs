using System.Collections.Concurrent;

namespace Std.App.Server.Queue;

/// <summary>
///     基于内存的队列提供器，使用 <see cref="ConcurrentQueue{T}" /> 实现消息存储
/// </summary>
internal sealed class MemoryQueueProvider : IQueueProvider
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<object>> _queues = new();

    /// <summary>
    ///     将消息异步入队到指定队列
    /// </summary>
    /// <typeparam name="T">消息载荷类型</typeparam>
    /// <param name="queue_name">队列名称</param>
    /// <param name="payload">消息载荷</param>
    /// <param name="priority">消息优先级，当前内存实现忽略此参数</param>
    /// <param name="delay">延迟投递时间，当前内存实现忽略此参数</param>
    public Task enqueue<T>(string queue_name, T payload, int? priority = null, TimeSpan? delay = null)
        where T : notnull
    {
        var queue = _queues.GetOrAdd(queue_name, _ => new ConcurrentQueue<object>());
        queue.Enqueue(payload);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     从指定队列中取出下一条消息，用于测试消费
    /// </summary>
    /// <typeparam name="T">消息载荷类型</typeparam>
    /// <param name="queue_name">队列名称</param>
    /// <returns>队列中的下一条消息，如果队列为空则返回默认值</returns>
    public static T? dequeue<T>(string queue_name)
    {
        if (_queues.TryGetValue(queue_name, out var queue))
            if (queue.TryDequeue(out var item) && item is T typed)
                return typed;

        return default;
    }
}