using System.Threading.Tasks;

namespace Core.Concurrency;

/// <summary>
///     通道接口，提供类型化的读写操作
/// </summary>
/// <typeparam name="T">通道传输的元素类型</typeparam>
public interface IChannel<T>
{
    /// <summary>
    ///     向通道写入一个元素
    /// </summary>
    /// <param name="item">要写入的元素</param>
    void write(T item);

    /// <summary>
    ///     异步从通道读取一个元素
    /// </summary>
    /// <returns>读取到的元素</returns>
    Task<T> read();
}