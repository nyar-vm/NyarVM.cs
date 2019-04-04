using Std.Video.Formats;

namespace Std.Video.Extensions;

/// <summary>
///     视频格式提供者接口，用于创建格式探测器和格式处理器。
/// </summary>
public interface IVideoFormatProvider
{
    /// <summary>
    ///     创建视频格式探测器集合。
    /// </summary>
    /// <returns>格式探测器集合。</returns>
    IEnumerable<IVideoFormatDetector> create_detectors();

    /// <summary>
    ///     创建视频格式处理器集合。
    /// </summary>
    /// <returns>格式处理器集合。</returns>
    IEnumerable<IVideoFormatHandler> create_handlers();
}