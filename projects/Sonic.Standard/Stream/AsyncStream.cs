using Core.Stream;

namespace Std.Stream;

/// <summary>
///     异步流，实现 IAsyncStream 接口，提供异步读写操作
/// </summary>
public sealed class AsyncStream : IAsyncStream
{
    /// <summary>
    ///     底层流
    /// </summary>
    private readonly System.IO.Stream _stream;

    /// <summary>
    ///     初始化异步流
    /// </summary>
    /// <param name="stream">底层流</param>
    public AsyncStream(System.IO.Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    ///     异步读取数据到缓冲区
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="offset">缓冲区偏移</param>
    /// <param name="count">读取字节数</param>
    /// <returns>实际读取的字节数</returns>
    public Task<int> read(byte[] buffer, int offset, int count)
    {
        return _stream.ReadAsync(buffer, offset, count);
    }

    /// <summary>
    ///     异步写入数据
    /// </summary>
    /// <param name="buffer">源缓冲区</param>
    /// <param name="offset">缓冲区偏移</param>
    /// <param name="count">写入字节数</param>
    /// <returns>异步任务</returns>
    public Task write(byte[] buffer, int offset, int count)
    {
        return _stream.WriteAsync(buffer, offset, count);
    }

    /// <summary>
    ///     异步刷新流
    /// </summary>
    /// <returns>异步任务</returns>
    public Task flush()
    {
        return _stream.FlushAsync();
    }
}