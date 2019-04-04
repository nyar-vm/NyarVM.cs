namespace Std.App.Server.Systems;

/// <summary>
///     Atlas 队列接口，提供后台任务入队操作。
/// </summary>
public interface IAtlasQueue
{
    /// <summary>
    ///     异步将任务入队
    /// </summary>
    /// <typeparam name="T">任务负载类型</typeparam>
    /// <param name="queueName">队列名称</param>
    /// <param name="payload">任务负载</param>
    /// <param name="priority">优先级</param>
    /// <param name="delay">延迟时间</param>
    Task EnqueueAsync<T>(string queueName, T payload, int? priority = null, TimeSpan? delay = null);
}