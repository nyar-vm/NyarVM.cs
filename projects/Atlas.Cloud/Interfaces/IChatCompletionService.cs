using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 聊天补全服务抽象接口，统一各类大语言模型 API 的操作
/// </summary>
public interface IChatCompletionService
{
    /// <summary>
    /// 执行聊天补全请求
    /// </summary>
    /// <param name="request">补全请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>补全响应</returns>
    Task<ChatResponse> complete(ChatRequest request, CancellationToken cancellationToken = default);
}