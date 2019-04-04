namespace Sonic.Audio.Analysis;

/// <summary>
///     梅尔频谱图类，存储音频帧的梅尔频率分析结果，用于语音识别和音频特征提取。
/// </summary>
public sealed class MelSpectrogram
{
    /// <summary>
    ///     初始化 <see cref="MelSpectrogram" /> 的新实例。
    /// </summary>
    /// <param name="frames">梅尔频谱帧数据。</param>
    /// <param name="mel_bins">梅尔频率仓数量。</param>
    /// <param name="frame_count">帧数量。</param>
    /// <param name="duration">分析时长。</param>
    public MelSpectrogram(float[][] frames, int mel_bins, int frame_count, double duration)
    {
        this.frames = frames;
        this.mel_bins = mel_bins;
        this.frame_count = frame_count;
        this.duration = duration;
    }

    /// <summary>
    ///     梅尔频谱帧数据，每帧包含一组梅尔频率仓的幅度值。
    /// </summary>
    public float[][] frames { get; }

    /// <summary>
    ///     梅尔频率仓数量。
    /// </summary>
    public int mel_bins { get; }

    /// <summary>
    ///     帧数量。
    /// </summary>
    public int frame_count { get; }

    /// <summary>
    ///     分析时长（秒）。
    /// </summary>
    public double duration { get; }
}