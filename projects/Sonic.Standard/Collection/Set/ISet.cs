namespace Std.Collection.Set;

/// <summary>无序唯一元素集合接口</summary>
/// <typeparam name="T">元素类型</typeparam>
public interface ISet<T>
{
    /// <summary>集合中元素的数量</summary>
    int count { get; }

    /// <summary>集合是否为空</summary>
    bool is_empty { get; }

    /// <summary>插入元素，若元素已存在则返回 <c>false</c></summary>
    bool insert(T value);

    /// <summary>移除元素，若元素不存在则返回 <c>false</c></summary>
    bool remove(T value);

    /// <summary>检查是否包含指定元�?/summary>
    bool contains(T value);

    /// <summary>清空所有元�?/summary>
    void clear();

    /// <summary>遍历所有元素并执行指定操作</summary>
    void for_each(Action<T> action);
}