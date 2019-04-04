using Std.Category;

namespace Std.Collection;

/// <summary>
///     固定容量的环形缓冲区，基于循环数组实现�?///
/// </summary>
/// <typeparam name="T">缓冲区中元素的类型�?/typeparam>
public class RingBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;

    /// <summary>
    ///     使用指定容量初始化环形缓冲区�?    ///
    /// </summary>
    /// <param name="capacity">
    ///     缓冲区的最大容量，必须大于零�?/param>
    ///     <exception cref="ArgumentOutOfRangeException">�?<paramref name="capacity" /> 小于或等于零时抛出�?/exception>
    public RingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "������������㡣");

        this.capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        count = 0;
    }

    /// <summary>
    ///     获取缓冲区中的元素数量�?    ///
    /// </summary>
    public int count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>
    ///     获取一个值，指示缓冲区是否为空�?    ///
    /// </summary>
    public bool is_empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count == 0;
    }

    /// <summary>
    ///     获取一个值，指示缓冲区是否已满�?    ///
    /// </summary>
    public bool is_full
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count == capacity;
    }

    /// <summary>
    ///     获取缓冲区的最大容量�?    ///
    /// </summary>
    public int capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    /// <summary>
    ///     将元素加入环形缓冲区尾部�?    ///
    /// </summary>
    /// <param name="value">
    ///     要加入的元素�?/param>
    ///     <returns>如果缓冲区未满并成功加入则返�?<c>true</c>，如果已满则返回 <c>false</c>�?/returns>
    public bool enqueue(T value)
    {
        if (count == capacity) return false;

        _buffer[_tail] = value;
        _tail = (_tail + 1) % capacity;
        count++;
        return true;
    }

    /// <summary>
    ///     从环形缓冲区头部取出元素�?    ///
    /// </summary>
    /// <returns>如果缓冲区非空则返回包含队首元素�?<see cref="Option{T}" />，否则返�?<see cref="Option{T}.none" />�?/returns>
    public Option<T> dequeue()
    {
        if (count == 0) return Option<T>.none;

        var value = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) % capacity;
        count--;
        return Option<T>.some(value);
    }

    /// <summary>
    ///     查看环形缓冲区头部元素但不取出�?    ///
    /// </summary>
    /// <returns>如果缓冲区非空则返回包含队首元素�?<see cref="Option{T}" />，否则返�?<see cref="Option{T}.none" />�?/returns>
    public Option<T> peek()
    {
        if (count == 0) return Option<T>.none;

        return Option<T>.some(_buffer[_head]);
    }

    /// <summary>
    ///     清空环形缓冲区中的所有元素�?    ///
    /// </summary>
    public void clear()
    {
        for (var i = 0; i < capacity; i++) _buffer[i] = default!;

        _head = 0;
        _tail = 0;
        count = 0;
    }
}