namespace Std.Image.Extensions;

/// <summary>
///     图像视觉请求类，封装视觉算子操作的参数。
/// </summary>
public sealed class ImageVisionRequest
{
    /// <summary>
    ///     获取或设置视觉算子标识键。
    /// </summary>
    public string key { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置附加参数字典。
    /// </summary>
    public Dictionary<string, object> parameters { get; set; } = [];
}