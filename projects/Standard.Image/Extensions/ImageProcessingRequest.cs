namespace Std.Image.Extensions;

/// <summary>
///     图像处理请求类，封装图像处理操作的参数。
/// </summary>
public sealed class ImageProcessingRequest
{
    /// <summary>
    ///     获取或设置处理操作标识键。
    /// </summary>
    public string key { get; set; } = string.Empty;

    /// <summary>
    ///     获取或设置目标宽度。
    /// </summary>
    public int target_width { get; set; }

    /// <summary>
    ///     获取或设置目标高度。
    /// </summary>
    public int target_height { get; set; }

    /// <summary>
    ///     获取或设置附加参数字典。
    /// </summary>
    public Dictionary<string, object> parameters { get; set; } = [];
}