using System.Threading.Tasks;

namespace Core.Stream;

/// <summary>
///     异步流接口，提供异步的读写和刷新操作
/// </summary>
public interface IAsyncStream
{
    /// <summary>
    ///     异步读取数据到缓冲区
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="offset">缓冲区偏移量</param>
    /// <param name="count">要读取的字节数</param>
    /// <returns>实际读取的字节数</returns>
    Task<int> read(byte[] buffer, int offset, int count);

    /// <summary>
    ///     异步写入缓冲区数据
    /// </summary>
    /// <param name="buffer">源缓冲区</param>
    /// <param name="offset">缓冲区偏移量</param>
    /// <param name="count">要写入的字节数</param>
    Task write(byte[] buffer, int offset, int count);

    /// <summary>
    ///     异步刷新流
    /// </summary>
    Task flush();
}