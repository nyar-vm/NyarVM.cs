namespace Std.Video.Vision;

/// <summary>
///     跟踪请求类，指定目标跟踪的参数。
/// </summary>
public sealed class TrackRequest
{
    /// <summary>
    ///     初始化 <see cref="TrackRequest" /> 的新实例。
    /// </summary>
    /// <param name="targetId">目标标识。</param>
    /// <param name="initialPosition">初始跟踪位置。</param>
    /// <param name="startFrame">起始帧索引。</param>
    /// <param name="endFrame">结束帧索引。</param>
    public TrackRequest(int targetId, Point2F initialPosition, int startFrame, int endFrame)
    {
        target_id = targetId;
        initial_position = initialPosition;
        start_frame = startFrame;
        end_frame = endFrame;
    }

    /// <summary>
    ///     获取目标标识。
    /// </summary>
    public int target_id { get; }

    /// <summary>
    ///     获取初始跟踪位置。
    /// </summary>
    public Point2F initial_position { get; }

    /// <summary>
    ///     获取起始帧索引。
    /// </summary>
    public int start_frame { get; }

    /// <summary>
    ///     获取结束帧索引。
    /// </summary>
    public int end_frame { get; }
}