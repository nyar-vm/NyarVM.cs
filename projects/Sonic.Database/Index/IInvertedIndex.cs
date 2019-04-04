namespace Olympus.Athena.Index;

/// <summary>
///     文档倒排索引接口，支持按词项检索文档集合
/// </summary>
public interface IInvertedIndex
{
    /// <summary>
    ///     向指定词项的倒排列表中添加文档 ID
    /// </summary>
    /// <param name="term">词项</param>
    /// <param name="docId">文档 ID</param>
    void Add(string term, long docId);

    /// <summary>
    ///     从指定词项的倒排列表中移除文档 ID
    /// </summary>
    /// <param name="term">词项</param>
    /// <param name="docId">文档 ID</param>
    void Remove(string term, long docId);

    /// <summary>
    ///     查询包含指定词项的文档 ID 集合
    /// </summary>
    /// <param name="term">词项</param>
    /// <returns>包含该词项的文档 ID 的只读集合</returns>
    IReadOnlySet<long> Search(string term);

    /// <summary>
    ///     查询同时包含多个词项的文档 ID 集合（交集）
    /// </summary>
    /// <param name="terms">词项列表</param>
    /// <returns>同时包含所有词项的文档 ID 的只读集合</returns>
    IReadOnlySet<long> Intersect(params string[] terms);

    /// <summary>
    ///     查询包含任一词项的文档 ID 集合（并集）
    /// </summary>
    /// <param name="terms">词项列表</param>
    /// <returns>包含任意词项的文档 ID 的只读集合</returns>
    IReadOnlySet<long> Union(params string[] terms);
}