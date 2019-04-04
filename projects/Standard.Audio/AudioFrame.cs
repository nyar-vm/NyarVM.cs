using Core.Media;
using Core.Media.Audio;

namespace Sonic.Audio;

/// <summary>
///     音频帧结构体，提供音频采样的时间切片数据，用于 A/V 同步和流式处理。
/// </summary>
/// <typeparam name="TSample">采样数据类型。</typeparam>
public readonly struct AudioFrame<TSample> : IAudio
    where TSample : unmanaged
{
    /// <summary>
    ///     采样数据数组。
    /// </summary>
    public readonly TSample[] samples;

    /// <summary>
    ///     获取采样率（Hz）。
    /// </summary>
    public int sample_rate { get; }

    /// <summary>
    ///     获取声道数。
    /// </summary>
    public int channels { get; }

    /// <summary>
    ///     获取采样格式。
    /// </summary>
    public SampleFormat format { get; }

    /// <summary>
    ///     获取声道布局。
    /// </summary>
    public ChannelLayout layout { get; }

    /// <summary>
    ///     获取时间戳，用于 A/V 同步。
    /// </summary>
    public long timestamp { get; }

    /// <summary>
    ///     获取音频帧时长（秒）。
    /// </summary>
    public double duration
    {
        get
        {
            if (sample_rate <= 0 || channels <= 0) return 0.0;

            return (double)samples.Length / (sample_rate * channels);
        }
    }

    /// <summary>
    ///     初始化 <see cref="AudioFrame{TSample}" /> 的新实例。
    /// </summary>
    /// <param name="sample_rate">采样率。</param>
    /// <param name="channels">声道数。</param>
    /// <param name="format">采样格式。</param>
    /// <param name="layout">声道布局。</param>
    /// <param name="timestamp">时间戳。</param>
    /// <param name="samples">采样数据。</param>
    public AudioFrame(int sample_rate, int channels, SampleFormat format, ChannelLayout layout, long timestamp,
        TSample[] samples)
    {
        this.sample_rate = sample_rate;
        this.channels = channels;
        this.format = format;
        this.layout = layout;
        this.timestamp = timestamp;
        this.samples = samples;
    }

    /// <summary>
    ///     获取采样数据的只读跨度。
    /// </summary>
    /// <returns>采样数据只读跨度。</returns>
    public ReadOnlySpan<TSample> readonly_span()
    {
        return samples.AsSpan();
    }

    /// <summary>
    ///     获取采样数据的可写跨度。
    /// </summary>
    /// <returns>采样数据跨度。</returns>
    public Span<TSample> span()
    {
        return samples.AsSpan();
    }
}