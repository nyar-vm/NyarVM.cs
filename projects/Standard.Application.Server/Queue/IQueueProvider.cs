namespace Std.App.Server.Queue;

/// <summary>
///     队列提供器接口，定义消息入队操作
/// </summary>
public interface IQueueProvider
{
    /// <summary>
    ///     将消息异步入队到指定队列
    /// </summary>
    /// <typeparam name="T">消息载荷类型</typeparam>
    /// <param name="queue_name">队列名称</param>
    /// <param name="payload">消息载荷</param>
    /// <param name="priority">消息优先级，数值越小优先级越高</param>
    /// <param name="delay">延迟投递时间</param>
    /// <returns>表示异步入队操作的任务</returns>
    Task enqueue<T>(string queue_name, T payload, int? priority = null, TimeSpan? delay = null)
        where T : notnull;
}