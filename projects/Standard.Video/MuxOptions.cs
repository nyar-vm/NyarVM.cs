namespace Std.Video;

/// <summary>
///     复用选项类，指定音视频复用的容器格式和参数。
/// </summary>
public sealed class MuxOptions
{
    /// <summary>
    ///     获取或设置容器格式名称。
    /// </summary>
    public string container_format { get; set; } = "";

    /// <summary>
    ///     获取或设置是否包含音频流。
    /// </summary>
    public bool include_audio { get; set; }

    /// <summary>
    ///     获取或设置音频偏移量（时间戳刻度）。
    /// </summary>
    public long audio_offset_ticks { get; set; }
}