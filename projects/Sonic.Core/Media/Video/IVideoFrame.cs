namespace Core.Media.Video;

/// <summary>
///     视频帧接口，提供视频帧的维度、格式和时间戳信息。
/// </summary>
public interface IVideoFrame
{
    /// <summary>
    ///     获取视频帧宽度（像素）。
    /// </summary>
    int width { get; }

    /// <summary>
    ///     获取视频帧高度（像素）。
    /// </summary>
    int height { get; }

    /// <summary>
    ///     获取像素格式。
    /// </summary>
    PixelFormat format { get; }

    /// <summary>
    ///     获取时间戳。
    /// </summary>
    long timestamp { get; }
}