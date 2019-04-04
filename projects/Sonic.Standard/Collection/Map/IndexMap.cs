using Std.Category;

namespace Std.Collection.Map;

/// <summary>
///     鍩轰簬绱㈠紩鏁扮粍鐨勬湁搴忛敭鍊煎鏄犲皠锛屾寜鎻掑叆椤哄簭缁存姢鍏冪礌锛屾敮鎸佹寜浣嶇疆璁块棶�?///
/// </summary>
/// <typeparam name="K">
///     閿被鍨嬨€?/typeparam>
///     <typeparam name="V">鍊肩被鍨嬨€?/typeparam>
public class IndexMap<K, V> : IMap<K, V> where K : notnull
{
    private readonly List<K> _keys;
    private readonly List<V> _values;

    /// <summary>
    ///     鍒濆鍖栦竴涓┖鐨?<see cref="IndexMap{K, V}" /> 瀹炰緥銆?    ///
    /// </summary>
    public IndexMap()
    {
        _keys = [];
        _values = [];
    }

    /// <summary>
    ///     鑾峰彇鎴栬缃寚瀹氫綅缃笂鐨勯敭鍊煎�?    ///
    /// </summary>
    /// <param name="index">浣嶇疆绱㈠紩�?/param>
    public (K Key, V Value) this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_keys[index], _values[index]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _keys[index] = value.Key;
            _values[index] = value.Value;
        }
    }

    /// <summary>
    ///     鑾峰彇鏄犲皠涓殑鍏冪礌鏁伴噺銆?    ///
    /// </summary>
    public int count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _keys.Count;
    }

    /// <summary>
    ///     鑾峰彇鏄犲皠鏄惁涓虹┖�?    ///
    /// </summary>
    public bool is_empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _keys.Count == 0;
    }

    /// <summary>
    ///     娓呯┖鏄犲皠涓殑鎵€鏈夊厓绱犮�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void clear()
    {
        _keys.Clear();
        _values.Clear();
    }

    /// <inheritdoc />
    public Option<V> get(K key)
    {
        var index = index_of_key(key);
        if (index >= 0) return Option<V>.some(_values[index]);

        return Option<V>.none;
    }

    /// <inheritdoc />
    public Option<V> insert(K key, V value)
    {
        var index = index_of_key(key);
        if (index >= 0)
        {
            var oldValue = _values[index];
            _values[index] = value;
            return Option<V>.some(oldValue);
        }

        _keys.Add(key);
        _values.Add(value);
        return Option<V>.none;
    }

    /// <inheritdoc />
    public Option<V> remove(K key)
    {
        var index = index_of_key(key);
        if (index >= 0)
        {
            var value = _values[index];
            _keys.RemoveAt(index);
            _values.RemoveAt(index);
            return Option<V>.some(value);
        }

        return Option<V>.none;
    }

    /// <inheritdoc />
    public bool contains_key(K key)
    {
        return index_of_key(key) >= 0;
    }

    /// <inheritdoc />
    public void for_each(Action<K, V> action)
    {
        for (var i = 0; i < _keys.Count; i++) action(_keys[i], _values[i]);
    }

    /// <inheritdoc />
    public IEnumerable<K> keys
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _keys;
    }

    /// <inheritdoc />
    public IEnumerable<V> values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _values;
    }

    private int index_of_key(K key)
    {
        for (var i = 0; i < _keys.Count; i++)
            if (EqualityComparer<K>.Default.Equals(_keys[i], key))
                return i;

        return -1;
    }

    /// <summary>
    ///     鑾峰彇鎸囧畾浣嶇疆涓婄殑閿紝濡傛灉绱㈠紩瓒婄晫鍒欐姏鍑?<see cref="ArgumentOutOfRangeException" />�?    ///
    /// </summary>
    /// <param name="index">
    ///     浣嶇疆绱㈠紩�?/param>
    ///     <returns>浣嶄�?<paramref name="index" /> 鐨勯敭銆?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public K get_key_at(int index)
    {
        return _keys[index];
    }

    /// <summary>
    ///     鑾峰彇鎸囧畾浣嶇疆涓婄殑鍊硷紝濡傛灉绱㈠紩瓒婄晫鍒欐姏鍑?<see cref="ArgumentOutOfRangeException" />�?    ///
    /// </summary>
    /// <param name="index">
    ///     浣嶇疆绱㈠紩�?/param>
    ///     <returns>浣嶄�?<paramref name="index" /> 鐨勫€笺�?/returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public V get_value_at(int index)
    {
        return _values[index];
    }
}