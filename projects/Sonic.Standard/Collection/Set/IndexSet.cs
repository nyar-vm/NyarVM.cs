using Std.Collection.Map;

namespace Std.Collection.Set;

/// <summary>
///     基于索引数组的有序集合，按插入顺序维护元素，使用 <see cref="IndexMap{T,Boolean}" /> 作为内部存储，支持按位置访问�?///
/// </summary>
/// <typeparam name="T">元素类型�?/typeparam>
public class IndexSet<T> : ISet<T>
    where T : notnull
{
    private readonly IndexMap<T, bool> _map;

    /// <summary>
    ///     初始化一个空�?<see cref="IndexSet{T}" /> 实例�?    ///
    /// </summary>
    public IndexSet()
    {
        _map = new IndexMap<T, bool>();
    }

    /// <inheritdoc />
    public int count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _map.count;
    }

    /// <inheritdoc />
    public bool is_empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _map.is_empty;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void clear()
    {
        _map.clear();
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool contains(T value)
    {
        return _map.contains_key(value);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool insert(T value)
    {
        return _map.insert(value, true).is_none;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool remove(T value)
    {
        return _map.remove(value).is_some;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void for_each(Action<T> action)
    {
        _map.for_each((key, _) => action(key));
    }

    /// <summary>
    ///     获取指定位置上的元素，如果索引越界则抛出 <see cref="ArgumentOutOfRangeException" />�?    ///
    /// </summary>
    /// <param name="index">
    ///     位置索引�?/param>
    ///     <returns>位于 <paramref name="index" /> 的元素�?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T get_at(int index)
    {
        return _map.get_key_at(index);
    }
}