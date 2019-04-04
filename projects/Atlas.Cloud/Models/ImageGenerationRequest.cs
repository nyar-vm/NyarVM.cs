namespace Atlas.Cloud.Models;

/// <summary>
/// 图像生成请求，封装模型、提示词和尺寸参数
/// </summary>
public sealed class ImageGenerationRequest
{
    /// <summary>
    /// 模型名称，如 "dall-e-3"
    /// </summary>
    public string model { get; init; } = string.Empty;

    /// <summary>
    /// 图像描述提示词
    /// </summary>
    public string prompt { get; init; } = string.Empty;

    /// <summary>
    /// 图像尺寸，默认 "1024x1024"
    /// </summary>
    public string size { get; init; } = "1024x1024";

    /// <summary>
    /// 生成图像数量，默认 1
    /// </summary>
    public int n { get; init; } = 1;
}
