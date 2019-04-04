namespace Std.Video;

/// <summary>
///     时间线类，提供帧索引与时间戳之间的双向转换。
/// </summary>
public sealed class Timeline
{
    /// <summary>
    ///     初始化 <see cref="Timeline" /> 的新实例。
    /// </summary>
    /// <param name="startTicks">起始时间戳。</param>
    /// <param name="endTicks">结束时间戳。</param>
    /// <param name="frameRate">帧率。</param>
    public Timeline(long startTicks, long endTicks, double frameRate)
    {
        start_ticks = startTicks;
        end_ticks = endTicks;
        frame_rate = frameRate;
    }

    /// <summary>
    ///     获取起始时间戳。
    /// </summary>
    public long start_ticks { get; }

    /// <summary>
    ///     获取结束时间戳。
    /// </summary>
    public long end_ticks { get; }

    /// <summary>
    ///     获取帧率。
    /// </summary>
    public double frame_rate { get; }

    /// <summary>
    ///     获取帧数。
    /// </summary>
    public int frame_count
    {
        get
        {
            if (frame_rate <= 0) return 0;

            var duration = end_ticks - start_ticks;

            return (int)(duration * frame_rate / 10_000_000.0);
        }
    }

    /// <summary>
    ///     根据帧索引计算对应的时间戳。
    /// </summary>
    /// <param name="frameIndex">帧索引。</param>
    /// <returns>对应的时间戳。</returns>
    public long ticks_from_frame(int frameIndex)
    {
        return start_ticks + (long)(frameIndex * 10_000_000.0 / frame_rate);
    }

    /// <summary>
    ///     根据时间戳计算对应的帧索引。
    /// </summary>
    /// <param name="ticks">时间戳。</param>
    /// <returns>对应的帧索引。</returns>
    public int frame_from_ticks(long ticks)
    {
        return (int)((ticks - start_ticks) * frame_rate / 10_000_000.0);
    }
}