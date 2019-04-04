namespace Std.Collection.Queue;

/// <summary>
///     队列接口
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IQueue<T> : IPushable<T>, IPopable<T>
{
    /// <summary>
    ///     入队
    /// </summary>
    /// <param name="value">要入队的元素</param>
    void enqueue(T value);

    /// <summary>
    ///     出队
    /// </summary>
    T dequeue();

    /// <summary>
    ///     查看队首元素但不移除
    /// </summary>
    new T peek();
}