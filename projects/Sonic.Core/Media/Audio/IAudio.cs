namespace Core.Media.Audio;

/// <summary>
///     音频接口，提供音频的基本参数信息。
/// </summary>
public interface IAudio
{
    /// <summary>
    ///     获取采样率（Hz）。
    /// </summary>
    int sample_rate { get; }

    /// <summary>
    ///     获取声道数。
    /// </summary>
    int channels { get; }

    /// <summary>
    ///     获取采样格式。
    /// </summary>
    SampleFormat format { get; }
}