using System.Threading.Channels;
using Core.Concurrency;

namespace Std.Concurrency;

/// <summary>
///     通道实现，实现 <see cref="IChannel{T}" /> 接口，
///     提供类型化的异步读写操作。
/// </summary>
/// <typeparam name="T">通道传输的元素类型。</typeparam>
public sealed class Channel<T> : IChannel<T>
{
    /// <summary>
    ///     内部系统通道。
    /// </summary>
    private readonly System.Threading.Channels.Channel<T> _inner;

    /// <summary>
    ///     初始化 <see cref="Channel{T}" /> 的新实例，创建无界通道。
    /// </summary>
    public Channel()
    {
        _inner = Channel.CreateUnbounded<T>();
    }

    /// <summary>
    ///     初始化 <see cref="Channel{T}" /> 的新实例，创建有界通道。
    /// </summary>
    /// <param name="capacity">通道容量。</param>
    public Channel(int capacity)
    {
        _inner = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>
    ///     向通道写入一个元素。
    /// </summary>
    /// <param name="item">要写入的元素。</param>
    public void write(T item)
    {
        _inner.Writer.TryWrite(item);
    }

    /// <summary>
    ///     异步从通道读取一个元素。
    /// </summary>
    /// <returns>读取到的元素。</returns>
    public async Task<T> read()
    {
        return await _inner.Reader.ReadAsync();
    }

    /// <summary>
    ///     异步向通道写入一个元素。
    /// </summary>
    /// <param name="item">要写入的元素。</param>
    /// <returns>异步任务。</returns>
    public async Task write_async(T item)
    {
        await _inner.Writer.WriteAsync(item);
    }

    /// <summary>
    ///     尝试向通道写入一个元素，不阻塞。
    /// </summary>
    /// <param name="item">要写入的元素。</param>
    /// <returns>是否写入成功。</returns>
    public bool try_write(T item)
    {
        return _inner.Writer.TryWrite(item);
    }
}