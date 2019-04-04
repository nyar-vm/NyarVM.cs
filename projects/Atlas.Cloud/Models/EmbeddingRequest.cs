namespace Atlas.Cloud.Models;

/// <summary>
/// 向量嵌入请求，封装模型、输入文本和维度参数
/// </summary>
public sealed class EmbeddingRequest
{
    /// <summary>
    /// 模型名称，如 "text-embedding-3-small"
    /// </summary>
    public string model { get; init; } = string.Empty;

    /// <summary>
    /// 输入文本列表
    /// </summary>
    public List<string> input { get; init; } = [];

    /// <summary>
    /// 输出向量维度，为 null 时使用模型默认值
    /// </summary>
    public int? dimensions { get; init; }
}
