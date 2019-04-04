using Core.Stream;

namespace Std.Stream;

/// <summary>
///     缓冲区写入器，实现 IBufferWriter&lt;T&gt; 接口，提供缓冲区写入操作
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public sealed class BufferWriter<T> : IBufferWriter<T>
{
    /// <summary>
    ///     缓冲区
    /// </summary>
    private T[] _buffer;

    /// <summary>
    ///     已写入的元素数量
    /// </summary>
    private int _written;

    /// <summary>
    ///     初始化缓冲区写入器
    /// </summary>
    /// <param name="initialCapacity">初始容量</param>
    public BufferWriter(int initialCapacity = 256)
    {
        _buffer = new T[initialCapacity];
        _written = 0;
    }

    /// <summary>
    ///     获取指定大小的缓冲区跨度
    /// </summary>
    /// <param name="sizeHint">请求的大小提示</param>
    /// <returns>缓冲区跨度</returns>
    public Span<T> get_span(int sizeHint = 0)
    {
        var needed = _written + System.Math.Max(sizeHint, 1);
        if (needed > _buffer.Length) Array.Resize(ref _buffer, System.Math.Max(needed, _buffer.Length * 2));
        return new Span<T>(_buffer, _written, _buffer.Length - _written);
    }

    /// <summary>
    ///     提交已写入的元素数量
    /// </summary>
    /// <param name="count">已写入的元素数量</param>
    public void advance(int count)
    {
        _written += count;
    }
}