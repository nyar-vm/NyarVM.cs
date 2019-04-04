using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Data.Search;

/// <summary>
///     搜索索引接口
/// </summary>
/// <typeparam name="T">文档类型</typeparam>
public interface ISearchIndex<T>
{
    /// <summary>
    ///     异步索引文档
    /// </summary>
    /// <param name="document">要索引的文档</param>
    Task index(T document);

    /// <summary>
    ///     异步搜索文档
    /// </summary>
    /// <param name="query">搜索查询</param>
    /// <returns>搜索结果列表</returns>
    Task<IReadOnlyList<T>> search(string query);
}