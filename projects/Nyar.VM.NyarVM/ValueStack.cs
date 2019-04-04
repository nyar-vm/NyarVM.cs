using System.Runtime.CompilerServices;
using Nyar.Types;

namespace Nyar.VM.NyarVM;

/// <summary>
///     值栈，支持动态扩容
/// </summary>
public sealed class ValueStack
{
    private const int _default_capacity = 1024;
    private const int _max_capacity = 1 << 20;
    private Value[] _items;

    /// <summary>
    ///     创建值栈
    /// </summary>
    /// <param name="capacity">初始容量。</param>
    public ValueStack(int capacity = _default_capacity)
    {
        _items = new Value[capacity];
        count = 0;
    }

    /// <summary>
    ///     栈中元素数量
    /// </summary>
    public int count { get; private set; }


    /// <summary>
    ///     当前容量
    /// </summary>
    public int capacity => _items.Length;

    /// <summary>
    ///     压栈
    /// </summary>
    /// <param name="value">值。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void push(Value value)
    {
        if (count >= _items.Length) grow();

        _items[count++] = value;
    }

    /// <summary>
    ///     出栈
    /// </summary>
    /// <returns>栈顶值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value pop()
    {
        if (count <= 0) throw new InvalidOperationException("值栈为空，无法出栈");

        return _items[--count];
    }

    /// <summary>
    ///     查看栈顶元素（不移除）
    /// </summary>
    /// <returns>栈顶值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value peek()
    {
        if (count <= 0) throw new InvalidOperationException("值栈为空，无法查看栈顶");

        return _items[count - 1];
    }

    /// <summary>
    ///     复制栈顶
    /// </summary>
    public void dup()
    {
        if (count <= 0) throw new InvalidOperationException("值栈为空");

        push(_items[count - 1]);
    }

    /// <summary>
    ///     交换栈顶两个元素
    /// </summary>
    public void swap()
    {
        if (count < 2) throw new InvalidOperationException("值栈元素不足");

        (_items[count - 1], _items[count - 2]) = (_items[count - 2], _items[count - 1]);
    }

    /// <summary>
    ///     获取栈中所有值（用于 GC Root 扫描）
    /// </summary>
    /// <returns>从栈底到栈顶的所有值。</returns>
    public IEnumerable<Value> get_all_values()
    {
        for (var i = 0; i < count; i++) yield return _items[i];
    }

    /// <summary>
    ///     创建值栈快照
    /// </summary>
    /// <returns>从栈底到栈顶的值数组。</returns>
    internal Value[] snapshot()
    {
        var copy = new Value[count];
        Array.Copy(_items, copy, count);
        return copy;
    }

    /// <summary>
    ///     清空值栈
    /// </summary>
    internal void clear()
    {
        Array.Clear(_items, 0, count);
        count = 0;
    }

    /// <summary>
    ///     动态扩容，容量翻倍
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void grow()
    {
        var newCapacity = _items.Length * 2;
        if (newCapacity > _max_capacity) newCapacity = _max_capacity;

        if (newCapacity <= _items.Length) throw new InvalidOperationException($"值栈容量已达上限 {_max_capacity}");

        var newItems = new Value[newCapacity];
        Array.Copy(_items, newItems, count);
        _items = newItems;
    }
}