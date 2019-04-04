using Atlas.Cloud.Models;

namespace Atlas.Cloud.Interfaces;

/// <summary>
/// AI 图像生成服务抽象接口
/// </summary>
public interface IImageGenerationService
{
    /// <summary>
    /// 执行图像生成请求
    /// </summary>
    /// <param name="request">图像生成请求参数</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>图像生成响应</returns>
    Task<ImageGenerationResponse> generate(ImageGenerationRequest request, CancellationToken ct = default);
}
