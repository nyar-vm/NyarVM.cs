using Std.Collection.Map;

namespace Std.Collection.Set;

/// <summary>
///     鍩轰簬绾㈤粦鏍戠殑鏈夊簭闆嗗悎锛屼娇�?<see cref="BTreeMap{T,Boolean}" /> 浣滀负鍐呴儴瀛樺偍銆?///
/// </summary>
/// <typeparam name="T">鍏冪礌绫诲瀷锛屽繀椤诲彲姣旇緝�?/typeparam>
public class BTreeSet<T> : ISet<T> where T : IComparable<T>
{
    private readonly BTreeMap<T, bool> _map;

    /// <summary>
    ///     鍒濆鍖栦竴涓┖鐨?<see cref="BTreeSet{T}" /> 瀹炰緥銆?    ///
    /// </summary>
    public BTreeSet()
    {
        _map = new BTreeMap<T, bool>();
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
}