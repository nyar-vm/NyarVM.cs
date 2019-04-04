namespace Sonic.Audio;

/// <summary>
///     时间范围结构体，表示以采样为单位的音频时间区间，用于分段和定位。
/// </summary>
public readonly struct TimeRange
{
    /// <summary>
    ///     获取起始采样索引。
    /// </summary>
    public long start_samples { get; }

    /// <summary>
    ///     获取结束采样索引（不含）。
    /// </summary>
    public long end_samples { get; }

    /// <summary>
    ///     获取采样率（Hz）。
    /// </summary>
    public int sample_rate { get; }

    /// <summary>
    ///     获取时间范围的时长（秒）。
    /// </summary>
    public double duration_seconds
    {
        get
        {
            if (sample_rate <= 0) return 0.0;

            return (double)length_samples / sample_rate;
        }
    }

    /// <summary>
    ///     获取时间范围的采样长度。
    /// </summary>
    public long length_samples => end_samples - start_samples;

    /// <summary>
    ///     初始化 <see cref="TimeRange" /> 的新实例。
    /// </summary>
    /// <param name="start_samples">起始采样索引。</param>
    /// <param name="end_samples">结束采样索引（不含）。</param>
    /// <param name="sample_rate">采样率。</param>
    public TimeRange(long start_samples, long end_samples, int sample_rate)
    {
        this.start_samples = start_samples;
        this.end_samples = end_samples;
        this.sample_rate = sample_rate;
    }
}