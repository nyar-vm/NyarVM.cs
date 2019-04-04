namespace Olympus.Athena.Cache;

#region TtlPolicy 基于 TTL 的驱逐策略

/// <summary>
///     基于 TTL 的驱逐策略，优先驱逐最接近过期或已过期的条目
/// </summary>
public sealed class TtlPolicy : IEvictionPolicy
{
    #region 构造函数

    /// <summary>
    ///     创建 TTL 驱逐策略实例
    /// </summary>
    public TtlPolicy()
    {
    }

    #endregion

    #region IEvictionPolicy 实现

    /// <inheritdoc />
    public CacheEntry? SelectVictim(IReadOnlyList<CacheEntry> entries)
    {
        CacheEntry? victim = null;
        var earliestExpiry = DateTime.MaxValue;

        foreach (var entry in entries)
        {
            DateTime expiry;
            if (entry.Ttl is not null)
                expiry = entry.CreatedAt + entry.Ttl.Value;
            else
                continue;

            if (expiry < earliestExpiry)
            {
                earliestExpiry = expiry;
                victim = entry;
            }
        }

        return victim;
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
    }

    #endregion
}

#endregion