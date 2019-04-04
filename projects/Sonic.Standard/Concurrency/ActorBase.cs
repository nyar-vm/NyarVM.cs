using System.Threading.Channels;
using Core.Concurrency;

namespace Std.Concurrency;

/// <summary>
///     Actor 模型抽象基类，实现 <see cref="IActor" /> 接口，
///     提供消息收发和消息循环的基础设施。
/// </summary>
public abstract class ActorBase : IActor
{
    /// <summary>
    ///     内部邮箱通道，用于接收消息。
    /// </summary>
    private readonly System.Threading.Channels.Channel<object> _mailbox =
        Channel.CreateUnbounded<object>();

    /// <summary>
    ///     异步接收并处理消息。
    /// </summary>
    /// <param name="message">接收到的消息对象。</param>
    public abstract Task receive(object message);

    /// <summary>
    ///     向 Actor 异步发送消息（不等待回复）。
    /// </summary>
    /// <param name="message">发送的消息对象。</param>
    /// <returns>异步任务。</returns>
    public async Task tell(object message)
    {
        await _mailbox.Writer.WriteAsync(message);
    }

    /// <summary>
    ///     向 Actor 异步发送消息并等待回复。
    /// </summary>
    /// <typeparam name="T">回复类型。</typeparam>
    /// <param name="message">发送的消息对象。</param>
    /// <returns>回复结果。</returns>
    public async Task<T> ask<T>(object message)
    {
        var tcs = new TaskCompletionSource<T>();
        await _mailbox.Writer.WriteAsync(message);
        return await tcs.Task;
    }

    /// <summary>
    ///     启动 Actor 的消息循环，持续从邮箱中读取消息并处理。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public async Task run(CancellationToken cancellationToken = default)
    {
        await foreach (var message in _mailbox.Reader.ReadAllAsync(cancellationToken)) await receive(message);
    }
}