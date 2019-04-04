namespace Olympus.Athena.Cache;

#region LruPolicy 最近最少使用驱逐策略

/// <summary>
///     最近最少使用（LRU）驱逐策略，优先驱逐最久未被访问的非过期条目
/// </summary>
public sealed class LruPolicy : IEvictionPolicy
{
    #region 构造函数

    /// <summary>
    ///     创建 LRU 驱逐策略实例
    /// </summary>
    public LruPolicy()
    {
        _order = new LinkedList<string>();
        _nodeMap = new Dictionary<string, LinkedListNode<string>>();
    }

    #endregion

    #region 内部状态

    private readonly LinkedList<string> _order;
    private readonly Dictionary<string, LinkedListNode<string>> _nodeMap;

    #endregion

    #region IEvictionPolicy 实现

    /// <inheritdoc />
    public CacheEntry? SelectVictim(IReadOnlyList<CacheEntry> entries)
    {
        for (var node = _order.Last; node is not null; node = node.Previous)
        {
            var matchingEntry = entries.FirstOrDefault(e => e.Key == node.Value);
            if (matchingEntry is not null && !matchingEntry.IsExpired) return matchingEntry;
        }

        return null;
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
        if (_nodeMap.TryGetValue(entry.Key, out var node))
            if (node.List is not null)
            {
                _order.Remove(node);
                _order.AddFirst(node);
            }
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
        var node = _order.AddFirst(entry.Key);
        _nodeMap[entry.Key] = node;
    }

    #endregion
}

#endregion