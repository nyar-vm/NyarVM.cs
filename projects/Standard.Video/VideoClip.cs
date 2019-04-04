using Core.Media;
using Std.Media;

namespace Std.Video;

/// <summary>
///     视频片段类，提供帧序列的存储、访问和裁剪能力。
/// </summary>
/// <typeparam name="TPixel">像素类型。</typeparam>
public sealed class VideoClip<TPixel>
    where TPixel : unmanaged
{
    private readonly List<VideoFrame<TPixel>> _frames;

    /// <summary>
    ///     初始化 <see cref="VideoClip{TPixel}" /> 的新实例。
    /// </summary>
    /// <param name="width">视频宽度。</param>
    /// <param name="height">视频高度。</param>
    /// <param name="format">像素格式。</param>
    /// <param name="frameRate">帧率。</param>
    public VideoClip(int width, int height, PixelFormat format, double frameRate)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        frame_rate = frameRate;
        _frames = [];
    }

    /// <summary>
    ///     获取视频宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取视频高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     获取像素格式。
    /// </summary>
    public PixelFormat format { get; }

    /// <summary>
    ///     获取帧数。
    /// </summary>
    public int frame_count => _frames.Count;

    /// <summary>
    ///     获取帧率。
    /// </summary>
    public double frame_rate { get; }

    /// <summary>
    ///     获取总时长（时间戳刻度）。
    /// </summary>
    public long duration_ticks
    {
        get
        {
            if (frame_rate <= 0) return 0;

            return (long)(frame_count * 10_000_000.0 / frame_rate);
        }
    }

    /// <summary>
    ///     获取总时长（秒）。
    /// </summary>
    public double duration_seconds
    {
        get
        {
            if (frame_rate <= 0) return 0.0;

            return frame_count / frame_rate;
        }
    }

    /// <summary>
    ///     获取帧的只读列表。
    /// </summary>
    public IReadOnlyList<VideoFrame<TPixel>> frames => _frames;

    /// <summary>
    ///     获取指定索引处的视频帧。
    /// </summary>
    /// <param name="index">帧索引。</param>
    /// <returns>指定索引处的视频帧。</returns>
    public VideoFrame<TPixel> get_frame(int index)
    {
        return _frames[index];
    }

    /// <summary>
    ///     追加一帧到视频片段末尾。
    /// </summary>
    /// <param name="frame">要追加的视频帧。</param>
    public void append(VideoFrame<TPixel> frame)
    {
        _frames.Add(frame);
    }

    /// <summary>
    ///     裁剪视频片段，返回指定时间范围内的子片段。
    /// </summary>
    /// <param name="startTicks">起始时间戳。</param>
    /// <param name="endTicks">结束时间戳。</param>
    /// <returns>裁剪后的视频片段。</returns>
    public VideoClip<TPixel> trim(long startTicks, long endTicks)
    {
        var result = new VideoClip<TPixel>(width, height, format, frame_rate);

        for (var i = 0; i < _frames.Count; i++)
        {
            var ts = _frames[i].timestamp;

            if (ts >= startTicks && ts < endTicks) result.append(_frames[i]);
        }

        return result;
    }
}