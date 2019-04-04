namespace Olympus.Athena.Cache;

#region UdfDrivenPolicy 用户自定义函数驱动的驱逐策略

/// <summary>
///     用户自定义函数（UDF）驱动的驱逐策略，通过外部权重函数决定驱逐优先级
/// </summary>
public sealed class UdfDrivenPolicy : IEvictionPolicy
{
    #region 内部状态

    private readonly Func<CacheEntry, double> _weightFunction;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建 UDF 驱动的驱逐策略实例
    /// </summary>
    /// <param name="weightFunction">权重计算函数，权重越低越优先被驱逐</param>
    public UdfDrivenPolicy(Func<CacheEntry, double> weightFunction)
    {
        _weightFunction = weightFunction;
    }

    #endregion

    #region IEvictionPolicy 实现

    /// <inheritdoc />
    public CacheEntry? SelectVictim(IReadOnlyList<CacheEntry> entries)
    {
        CacheEntry? victim = null;
        var minWeight = double.MaxValue;

        foreach (var entry in entries)
        {
            if (entry.IsExpired) continue;

            var weight = _weightFunction(entry);
            if (weight < minWeight)
            {
                minWeight = weight;
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