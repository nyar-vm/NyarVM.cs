using Core.Media.Video;

namespace Std.Video.Extensions;

/// <summary>
///     视频处理器接口，提供可扩展的视频帧处理能力。
/// </summary>
public interface IVideoProcessor
{
    /// <summary>
    ///     获取处理器标识键。
    /// </summary>
    string key { get; }

    /// <summary>
    ///     判断当前处理器是否能处理指定的视频帧。
    /// </summary>
    /// <param name="frame">待处理的视频帧。</param>
    /// <returns>是否能处理。</returns>
    bool can_process(IVideoFrame frame);

    /// <summary>
    ///     处理视频帧。
    /// </summary>
    /// <param name="frame">待处理的视频帧。</param>
    /// <returns>处理后的视频帧。</returns>
    IVideoFrame process(IVideoFrame frame);
}