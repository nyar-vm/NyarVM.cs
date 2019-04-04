namespace Atlas.Cloud.Models;

/// <summary>
/// 搜索结果，包含匹配总数和命中列表
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// 匹配文档总数
    /// </summary>
    public int total { get; init; }

    /// <summary>
    /// 命中的文档列表
    /// </summary>
    public List<SearchHit> hits { get; init; } = [];

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="total">匹配总数</param>
    /// <param name="hits">命中列表</param>
    public static SearchResult ok(int total, List<SearchHit> hits) =>
        new() { total = total, hits = hits };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误消息</param>
    public static SearchResult fail(string error) =>
        new() { error = error };
}
