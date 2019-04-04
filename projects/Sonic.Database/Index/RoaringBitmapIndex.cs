namespace Olympus.Athena.Index;

#region RoaringBitmapIndex 倒排索引

/// <summary>
///     基于简化 RoaringBitmap 的倒排索引实现，使用 <see cref="HashSet{T}" /> 替代容器位图存储倒排列表
/// </summary>
public sealed class RoaringBitmapIndex : IInvertedIndex
{
    #region 字段

    private readonly Dictionary<string, HashSet<long>> _invertedLists;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建空的倒排索引
    /// </summary>
    public RoaringBitmapIndex()
    {
        _invertedLists = new Dictionary<string, HashSet<long>>();
    }

    #endregion

    #region 添加倒排项

    /// <summary>
    ///     向指定词项的倒排列表中添加文档 ID
    /// </summary>
    /// <param name="term">词项</param>
    /// <param name="docId">文档 ID</param>
    public void Add(string term, long docId)
    {
        if (!_invertedLists.TryGetValue(term, out var list))
        {
            list = [];
            _invertedLists[term] = list;
        }

        list.Add(docId);
    }

    #endregion

    #region 移除倒排项

    /// <summary>
    ///     从指定词项的倒排列表中移除文档 ID
    /// </summary>
    /// <param name="term">词项</param>
    /// <param name="docId">文档 ID</param>
    public void Remove(string term, long docId)
    {
        if (_invertedLists.TryGetValue(term, out var list)) list.Remove(docId);
    }

    #endregion

    #region 单 Term 搜索

    /// <summary>
    ///     查询包含指定词项的文档 ID 集合
    /// </summary>
    /// <param name="term">词项</param>
    /// <returns>包含该词项的文档 ID 的只读集合</returns>
    public IReadOnlySet<long> Search(string term)
    {
        if (_invertedLists.TryGetValue(term, out var list)) return new HashSet<long>(list);

        return new HashSet<long>();
    }

    #endregion

    #region 多 Term 交集

    /// <summary>
    ///     查询同时包含多个词项的文档 ID 集合（交集），取最小列表遍历检查是否在所有其他列表中存在
    /// </summary>
    /// <param name="terms">词项列表</param>
    /// <returns>同时包含所有词项的文档 ID 的只读集合</returns>
    public IReadOnlySet<long> Intersect(params string[] terms)
    {
        if (terms.Length == 0) return new HashSet<long>();

        var candidates = new List<HashSet<long>>();
        foreach (var term in terms)
            if (_invertedLists.TryGetValue(term, out var list))
                candidates.Add(list);
            else
                return new HashSet<long>();

        candidates.Sort((a, b) => a.Count.CompareTo(b.Count));

        var result = new HashSet<long>();
        var first = candidates[0];
        foreach (var docId in first)
        {
            var inAll = true;
            for (var i = 1; i < candidates.Count; i++)
                if (!candidates[i].Contains(docId))
                {
                    inAll = false;
                    break;
                }

            if (inAll) result.Add(docId);
        }

        return result;
    }

    #endregion

    #region 多 Term 并集

    /// <summary>
    ///     查询包含任一词项的文档 ID 集合（并集），合并所有列表到 HashSet
    /// </summary>
    /// <param name="terms">词项列表</param>
    /// <returns>包含任意词项的文档 ID 的只读集合</returns>
    public IReadOnlySet<long> Union(params string[] terms)
    {
        var result = new HashSet<long>();
        foreach (var term in terms)
            if (_invertedLists.TryGetValue(term, out var list))
                result.UnionWith(list);

        return result;
    }

    #endregion
}

#endregion