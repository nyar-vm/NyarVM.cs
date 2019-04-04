using Std.Math;

namespace Std.DataProcess.Write;

/// <summary>
///     基于数组的缓冲区写入器，语义上等价于 <c>System.Buffers.ArrayBufferWriter&lt;T&gt;</c>�?/// 内部维护一个连续数组，按需扩容，适用于序列化等需要逐步写入的场景�?///
/// </summary>
/// <typeparam name="T">写入元素的类型�?/typeparam>
public sealed class ArrayBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[] _buffer;

    /// <summary>
    ///     使用默认初始容量�?56）初始化写入器�?    ///
    /// </summary>
    public ArrayBufferWriter()
    {
        _buffer = new T[256];
        written_count = 0;
    }

    /// <summary>
    ///     使用指定的初始容量初始化写入器�?    ///
    /// </summary>
    /// <param name="initial_capacity">初始缓冲区容量�?/param>
    public ArrayBufferWriter(int initialCapacity)
    {
        if (initialCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity), "初始容量必须大于零。");

        _buffer = new T[initialCapacity];
        written_count = 0;
    }

    /// <summary>
    ///     已写入的元素数量�?    ///
    /// </summary>
    public int written_count { get; private set; }

    /// <summary>
    ///     当前缓冲区的总容量�?    ///
    /// </summary>
    public int capacity => _buffer.Length;

    /// <summary>
    ///     获取已写入数据的不可变视图�?    ///
    /// </summary>
    public ReadOnlyMemory<T> written_memory => _buffer.AsMemory(0, written_count);

    /// <summary>
    ///     获取已写入数据的不可变跨度�?    ///
    /// </summary>
    public ReadOnlySpan<T> written_span => _buffer.AsSpan(0, written_count);

    /// <inheritdoc />
    public void advance(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "前进数量不能为负数。");

        if (written_count > _buffer.Length - count) throw new InvalidOperationException("前进数量超出已获取的缓冲区大小。");

        written_count += count;
    }

    /// <inheritdoc />
    public Memory<T> get_memory(int sizeHint = 0)
    {
        check_and_resize_buffer(sizeHint);
        return _buffer.AsMemory(written_count);
    }

    /// <inheritdoc />
    public Span<T> get_span(int sizeHint = 0)
    {
        check_and_resize_buffer(sizeHint);
        return _buffer.AsSpan(written_count);
    }

    /// <summary>
    ///     释放写入器使用的资源。
    /// </summary>
    public void Dispose()
    {
        clear();
    }

    /// <summary>
    ///     清空已写入的数据，重置写入位置。
    /// </summary>
    public void clear()
    {
        _buffer.AsSpan(0, written_count).Clear();
        written_count = 0;
    }

    /// <summary>
    ///     检查并扩容缓冲区�?    ///
    /// </summary>
    private void check_and_resize_buffer(int sizeHint)
    {
        if (sizeHint < 0) throw new ArgumentOutOfRangeException(nameof(sizeHint), "大小提示不能为负数。");

        if (sizeHint == 0) sizeHint = 1;

        if (sizeHint > _buffer.Length - written_count)
        {
            var growBy = SonicMath.max(sizeHint, _buffer.Length);

            if (_buffer.Length + growBy > int.MaxValue) growBy = int.MaxValue - _buffer.Length;

            var newBuffer = new T[_buffer.Length + growBy];
            Array.Copy(_buffer, newBuffer, written_count);
            _buffer = newBuffer;
        }
    }
}