namespace Atlas.Cloud.Models;

/// <summary>
/// 图像生成响应，包含生成的图像数据
/// </summary>
public sealed class ImageGenerationResponse
{
    /// <summary>
    /// 实际使用的模型名称
    /// </summary>
    public string model { get; init; } = string.Empty;

    /// <summary>
    /// 生成的图像数据列表
    /// </summary>
    public List<byte[]> images { get; init; } = [];

    /// <summary>
    /// 错误消息，成功时为空
    /// </summary>
    public string? error { get; init; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="model">模型名称</param>
    /// <param name="images">图像数据列表</param>
    public static ImageGenerationResponse ok(string model, List<byte[]> images) =>
        new() { model = model, images = images };

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="error">错误消息</param>
    public static ImageGenerationResponse fail(string error) =>
        new() { error = error };
}
