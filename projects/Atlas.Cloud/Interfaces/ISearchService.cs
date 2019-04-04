using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 全文搜索服务抽象接口
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 执行搜索请求
    /// </summary>
    /// <param name="request">搜索请求参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<SearchResult> search(SearchRequest request, CancellationToken ct = default);

    /// <summary>
    /// 索引文档
    /// </summary>
    /// <param name="indexName">索引名称</param>
    /// <param name="documentId">文档唯一标识</param>
    /// <param name="document">文档数据</param>
    /// <param name="ct">取消令牌</param>
    Task index(string indexName, string documentId, byte[] document, CancellationToken ct = default);

    /// <summary>
    /// 删除索引中的文档
    /// </summary>
    /// <param name="indexName">索引名称</param>
    /// <param name="documentId">文档唯一标识</param>
    /// <param name="ct">取消令牌</param>
    Task delete_index(string indexName, string documentId, CancellationToken ct = default);
}
