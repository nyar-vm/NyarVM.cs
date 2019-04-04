using Core.Media.Image;

namespace Std.Image.Extensions;

/// <summary>
///     图像视觉算子接口，用于扩展传统视觉算法。
/// </summary>
public interface IImageVisionOperator
{
    /// <summary>
    ///     获取算子标识键。
    /// </summary>
    string key { get; }

    /// <summary>
    ///     判断是否可以执行指定的视觉请求。
    /// </summary>
    /// <param name="request">图像视觉请求。</param>
    /// <returns>是否可以执行。</returns>
    bool can_execute(ImageVisionRequest request);

    /// <summary>
    ///     执行视觉算子。
    /// </summary>
    /// <param name="inputs">输入图像列表。</param>
    /// <param name="request">图像视觉请求。</param>
    /// <returns>算子执行结果。</returns>
    object execute(IReadOnlyList<IImage> inputs, ImageVisionRequest request);
}