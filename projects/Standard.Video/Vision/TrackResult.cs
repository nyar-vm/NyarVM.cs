namespace Std.Video.Vision;

/// <summary>
///     跟踪结果类，存储目标在视频序列中的跟踪轨迹。
/// </summary>
public sealed class TrackResult
{
    /// <summary>
    ///     初始化 <see cref="TrackResult" /> 的新实例。
    /// </summary>
    /// <param name="trackId">目标跟踪标识。</param>
    /// <param name="positions">跟踪位置序列。</param>
    /// <param name="timestamps">对应的时间戳序列。</param>
    /// <param name="isLost">目标是否已丢失跟踪。</param>
    public TrackResult(int trackId, Point2F[] positions, long[] timestamps, bool isLost)
    {
        track_id = trackId;
        this.positions = positions;
        this.timestamps = timestamps;
        is_lost = isLost;
    }

    /// <summary>
    ///     获取目标跟踪标识。
    /// </summary>
    public int track_id { get; }

    /// <summary>
    ///     获取跟踪位置序列。
    /// </summary>
    public Point2F[] positions { get; }

    /// <summary>
    ///     获取对应的时间戳序列。
    /// </summary>
    public long[] timestamps { get; }

    /// <summary>
    ///     获取目标是否已丢失跟踪。
    /// </summary>
    public bool is_lost { get; }

    /// <summary>
    ///     获取跟踪帧数。
    /// </summary>
    public int frame_count => positions.Length;
}