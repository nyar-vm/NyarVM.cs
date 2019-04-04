namespace Atlas.Cloud.Models;

/// <summary>
/// 搜索命中项，包含文档标识、相关度评分和原始数据
/// </summary>
public sealed class SearchHit
{
    /// <summary>
    /// 文档唯一标识
    /// </summary>
    public string id { get; init; } = string.Empty;

    /// <summary>
    /// 相关度评分
    /// </summary>
    public double score { get; init; }

    /// <summary>
    /// 文档原始数据，为 null 时表示未存储源文档
    /// </summary>
    public byte[]? source { get; init; }
}
