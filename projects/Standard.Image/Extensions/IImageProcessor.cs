using Core.Media.Image;

namespace Std.Image.Extensions;

/// <summary>
///     图像处理器接口，用于扩展图像处理算子。
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    ///     获取处理器标识键。
    /// </summary>
    string key { get; }

    /// <summary>
    ///     判断是否可以处理指定的请求。
    /// </summary>
    /// <param name="request">图像处理请求。</param>
    /// <returns>是否可以处理。</returns>
    bool can_process(ImageProcessingRequest request);

    /// <summary>
    ///     执行图像处理。
    /// </summary>
    /// <param name="source">源图像。</param>
    /// <param name="request">图像处理请求。</param>
    /// <returns>处理后的图像。</returns>
    IImage process(IImage source, ImageProcessingRequest request);
}