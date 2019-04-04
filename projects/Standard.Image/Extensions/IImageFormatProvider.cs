using Std.Image.Formats;

namespace Std.Image.Extensions;

/// <summary>
///     图像格式提供者接口，用于创建格式探测器和格式处理器。
/// </summary>
public interface IImageFormatProvider
{
    /// <summary>
    ///     创建格式探测器列表。
    /// </summary>
    /// <returns>格式探测器列表。</returns>
    IEnumerable<IImageFormatDetector> create_detectors();

    /// <summary>
    ///     创建格式处理器列表。
    /// </summary>
    /// <returns>格式处理器列表。</returns>
    IEnumerable<IImageFormatHandler> create_handlers();
}