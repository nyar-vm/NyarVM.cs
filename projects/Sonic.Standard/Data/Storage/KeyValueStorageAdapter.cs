using Std.DataStorage.Key;

namespace Std.Data.Storage;

/// <summary>
///     基于 <see cref="Dictionary{TKey,TValue}" /> 的键值存储适配器，提供内存中的键值对存取操作。
/// </summary>
public sealed class KeyValueStorageAdapter : IKeyValueStorage
{
    private readonly Dictionary<object, object?> _store = new();

    /// <summary>
    ///     写入键值对。
    /// </summary>
    /// <typeparam name="K">键类型。</typeparam>
    /// <typeparam name="V">值类型。</typeparam>
    /// <param name="key">键。</param>
    /// <param name="value">值。</param>
    public void put<K, V>(in K key, in V value)
        where K : notnull
    {
        _store[key] = value;
    }

    /// <summary>
    ///     根据键读取值。
    /// </summary>
    /// <typeparam name="K">键类型。</typeparam>
    /// <typeparam name="V">值类型。</typeparam>
    /// <param name="key">键。</param>
    /// <returns>找到的值，未找到时为 <c>null</c>。</returns>
    public V? get<K, V>(in K key)
        where K : notnull
    {
        if (!_store.TryGetValue(key, out var value)) return default;

        return (V?)value;
    }

    /// <summary>
    ///     根据键删除键值对。
    /// </summary>
    /// <typeparam name="K">键类型。</typeparam>
    /// <param name="key">键。</param>
    public void delete<K>(in K key)
        where K : notnull
    {
        _store.Remove(key);
    }
}