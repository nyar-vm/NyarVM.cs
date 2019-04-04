using Std.Category;

namespace Std.Collection.Map;

/// <summary>键值对映射集合接口</summary>
/// <typeparam name="K">
///     键类�?/typeparam>
///     <typeparam name="V">值类�?/typeparam>
public interface IMap<K, V>
{
    /// <summary>映射中键值对的数�?/summary>
    int count { get; }

    /// <summary>映射是否为空</summary>
    bool is_empty { get; }

    /// <summary>映射中所有键的集�?/summary>
    IEnumerable<K> keys { get; }

    /// <summary>映射中所有值的集合</summary>
    IEnumerable<V> values { get; }

    /// <summary>获取指定键对应的�?/summary>
    Option<V> get(K key);

    /// <summary>插入键值对，若键已存在则返回旧�?/summary>
    Option<V> insert(K key, V value);

    /// <summary>移除指定键并返回被移除的�?/summary>
    Option<V> remove(K key);

    /// <summary>检查是否包含指定键</summary>
    bool contains_key(K key);

    /// <summary>清空所有键值对</summary>
    void clear();

    /// <summary>遍历所有键值对并执行指定操�?/summary>
    void for_each(Action<K, V> action);
}