using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// AI 向量嵌入服务抽象接口
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// 执行向量嵌入请求
    /// </summary>
    /// <param name="request">嵌入请求参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>嵌入响应</returns>
    Task<EmbeddingResponse> embed(EmbeddingRequest request, CancellationToken ct = default);
}
