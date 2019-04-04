namespace Olympus.Athena.Cache;

#region IEvictionPolicy 驱逐策略接口

/// <summary>
///     缓存驱逐策略接口，定义选择牺牲条目和响应访问/添加事件的契约
/// </summary>
public interface IEvictionPolicy
{
    /// <summary>
    ///     从候选条目中选择一个最应该被驱逐的条目
    /// </summary>
    /// <param name="entries">当前缓存中的所有条目</param>
    /// <returns>被选中的驱逐条目，无合适条目时返回 <c>null</c></returns>
    CacheEntry? SelectVictim(IReadOnlyList<CacheEntry> entries);

    /// <summary>
    ///     条目被访问时回调
    /// </summary>
    /// <param name="entry">被访问的条目</param>
    void OnAccess(CacheEntry entry);

    /// <summary>
    ///     条目被添加到缓存时回调
    /// </summary>
    /// <param name="entry">被添加的条目</param>
    void OnAdd(CacheEntry entry);
}

#endregion