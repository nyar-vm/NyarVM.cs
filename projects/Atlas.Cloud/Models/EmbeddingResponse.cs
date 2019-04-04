namespace Atlas.Cloud.Models;

/// <summary>
/// 向量嵌入响应，包含嵌入向量和用量信息
/// </summary>
public sealed class EmbeddingResponse
{
    /// <summary>
    /// 实际使用的模型名称
    /// </summary>
    public string model { get; init; } = string.Empty;

    /// <summary>
    /// 嵌入向量列表，与输入文本一一对应
    /// </summary>
    public List<float[]> embeddings { get; init; } = [];

    /// <summary>
    /// 总令牌用量
    /// </summary>
    public int usage_total_tokens { get; init; }

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="model">模型名称</param>
    /// <param name="embeddings">嵌入向量列表</param>
    /// <param name="tokens">令牌用量</param>
    public static EmbeddingResponse ok(string model, List<float[]> embeddings, int tokens) =>
        new() { model = model, embeddings = embeddings, usage_total_tokens = tokens };

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="error">错误消息</param>
    public static EmbeddingResponse fail(string error) =>
        new() { error = error };
}
