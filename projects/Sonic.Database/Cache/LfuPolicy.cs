namespace Olympus.Athena.Cache;

#region LfuPolicy 最不频繁使用驱逐策略

/// <summary>
///     最不频繁使用（LFU）驱逐策略，优先驱逐访问次数最少的非过期条目
/// </summary>
public sealed class LfuPolicy : IEvictionPolicy
{
    #region 构造函数

    /// <summary>
    ///     创建 LFU 驱逐策略实例
    /// </summary>
    public LfuPolicy()
    {
    }

    #endregion

    #region IEvictionPolicy 实现

    /// <inheritdoc />
    public CacheEntry? SelectVictim(IReadOnlyList<CacheEntry> entries)
    {
        CacheEntry? victim = null;
        var minCount = long.MaxValue;

        foreach (var entry in entries)
        {
            if (entry.IsExpired) continue;

            if (entry.AccessCount < minCount)
            {
                minCount = entry.AccessCount;
                victim = entry;
            }
        }

        return victim;
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
        entry.AccessCount++;
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
        entry.AccessCount = 1L;
    }

    #endregion
}

#endregion