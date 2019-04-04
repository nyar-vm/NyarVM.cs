using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// 翻译服务抽象接口
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// 执行翻译请求
    /// </summary>
    /// <param name="request">翻译请求参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>翻译结果</returns>
    Task<TranslationResult> translate(TranslationRequest request, CancellationToken ct = default);
}
