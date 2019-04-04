namespace Atlas.Cloud.Models;

/// <summary>
/// 搜索请求，封装索引名称、查询语句和分页参数
/// </summary>
public sealed class SearchRequest
{
    /// <summary>
    /// 索引名称
    /// </summary>
    public string index_name { get; init; } = string.Empty;

    /// <summary>
    /// 查询语句
    /// </summary>
    public string query { get; init; } = string.Empty;

    /// <summary>
    /// 返回结果数量上限，默认 10
    /// </summary>
    public int limit { get; init; } = 10;

    /// <summary>
    /// 结果偏移量，默认 0
    /// </summary>
    public int offset { get; init; } = 0;
}
