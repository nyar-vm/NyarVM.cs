namespace Std.App.Server.Queue;

/// <summary>
///     队列消费者接口，定义从队列异步消费消息的操作
/// </summary>
/// <typeparam name="T">消息载荷类型</typeparam>
public interface IQueueConsumer<T>
{
    /// <summary>
    ///     异步消费队列中的消息
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>消息的异步可枚举序列</returns>
    IAsyncEnumerable<T> consume(CancellationToken ct = default);
}