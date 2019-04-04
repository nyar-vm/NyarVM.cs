namespace Std.Video;

/// <summary>
///     音视频同步上下文，跟踪视频和音频时间戳的偏移与漂移。
/// </summary>
public sealed class AvSyncContext
{
    private const long _default_sync_threshold_ticks = 500_000;

    /// <summary>
    ///     获取视频时间戳。
    /// </summary>
    public long video_timestamp { get; private set; }

    /// <summary>
    ///     获取音频时间戳。
    /// </summary>
    public long audio_timestamp { get; private set; }

    /// <summary>
    ///     获取时钟偏移量。
    /// </summary>
    public long clock_offset { get; private set; }

    /// <summary>
    ///     获取音视频漂移量，即视频时间戳与音频时间戳的差值。
    /// </summary>
    public double drift => video_timestamp - audio_timestamp - clock_offset;

    /// <summary>
    ///     获取音视频是否同步，漂移量在阈值范围内即为同步。
    /// </summary>
    public bool is_synchronized
    {
        get
        {
            var absDrift = drift;

            if (absDrift < 0) absDrift = -absDrift;

            return absDrift < _default_sync_threshold_ticks;
        }
    }

    /// <summary>
    ///     更新视频时间戳。
    /// </summary>
    /// <param name="timestamp">新的视频时间戳。</param>
    public void update_video(long timestamp)
    {
        video_timestamp = timestamp;
    }

    /// <summary>
    ///     更新音频时间戳。
    /// </summary>
    /// <param name="timestamp">新的音频时间戳。</param>
    public void update_audio(long timestamp)
    {
        audio_timestamp = timestamp;
    }

    /// <summary>
    ///     重置同步上下文，将所有时间戳和偏移量归零。
    /// </summary>
    public void reset()
    {
        video_timestamp = 0;
        audio_timestamp = 0;
        clock_offset = 0;
    }
}