using Core.Media;
using Core.Media.Audio;

namespace Sonic.Audio;

/// <summary>
///     音频片段类，提供可变的音频采样数据容器，支持追加、切片和帧提取操作。
/// </summary>
/// <typeparam name="TSample">采样数据类型。</typeparam>
public sealed class AudioClip<TSample> : IAudio
    where TSample : unmanaged
{
    /// <summary>
    ///     采样数据数组。
    /// </summary>
    public TSample[] samples;

    /// <summary>
    ///     初始化 <see cref="AudioClip{TSample}" /> 的新实例。
    /// </summary>
    /// <param name="sample_rate">采样率。</param>
    /// <param name="channels">声道数。</param>
    /// <param name="format">采样格式。</param>
    /// <param name="layout">声道布局。</param>
    public AudioClip(int sample_rate, int channels, SampleFormat format, ChannelLayout layout)
    {
        this.sample_rate = sample_rate;
        this.channels = channels;
        this.format = format;
        this.layout = layout;
        samples = [];
    }

    /// <summary>
    ///     初始化 <see cref="AudioClip{TSample}" /> 的新实例，使用已有采样数据。
    /// </summary>
    /// <param name="sample_rate">采样率。</param>
    /// <param name="channels">声道数。</param>
    /// <param name="format">采样格式。</param>
    /// <param name="layout">声道布局。</param>
    /// <param name="samples">采样数据。</param>
    public AudioClip(int sample_rate, int channels, SampleFormat format, ChannelLayout layout, TSample[] samples)
    {
        this.sample_rate = sample_rate;
        this.channels = channels;
        this.format = format;
        this.layout = layout;
        this.samples = samples;
    }

    /// <summary>
    ///     获取声道布局。
    /// </summary>
    public ChannelLayout layout { get; }

    /// <summary>
    ///     获取音频片段时长（秒）。
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
    ///     获取总采样数（每声道）。
    /// </summary>
    public long sample_count
    {
        get
        {
            if (channels <= 0) return 0;

            return samples.Length / channels;
        }
    }

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
    ///     从指定起始位置提取指定长度的音频帧。
    /// </summary>
    /// <param name="start">起始采样索引（交错布局下为绝对索引）。</param>
    /// <param name="length">帧长度（采样数，含所有声道）。</param>
    /// <returns>提取的音频帧。</returns>
    public AudioFrame<TSample> get_frame(long start, int length)
    {
        var available = samples.Length - (int)start;
        var actualLength = Math.Min(length, available);

        if (actualLength <= 0)
            return new AudioFrame<TSample>(sample_rate, channels, format, layout, start, []);

        var frameSamples = new TSample[actualLength];
        Array.Copy(samples, (int)start, frameSamples, 0, actualLength);

        return new AudioFrame<TSample>(sample_rate, channels, format, layout, start, frameSamples);
    }

    /// <summary>
    ///     将音频帧的采样数据追加到当前片段末尾。
    /// </summary>
    /// <param name="frame">要追加的音频帧。</param>
    public void append(AudioFrame<TSample> frame)
    {
        var oldLength = samples.Length;
        Array.Resize(ref samples, oldLength + frame.samples.Length);
        Array.Copy(frame.samples, 0, samples, oldLength, frame.samples.Length);
    }

    /// <summary>
    ///     从当前片段中截取指定范围的子片段。
    /// </summary>
    /// <param name="start_sample">起始采样索引（每声道）。</param>
    /// <param name="end_sample">结束采样索引（每声道，不含）。</param>
    /// <returns>截取的子片段。</returns>
    public AudioClip<TSample> slice(long start_sample, long end_sample)
    {
        var startOffset = (int)(start_sample * channels);
        var endOffset = (int)(end_sample * channels);
        var length = endOffset - startOffset;

        if (length <= 0) return new AudioClip<TSample>(sample_rate, channels, format, layout);

        var slicedSamples = new TSample[length];
        Array.Copy(samples, startOffset, slicedSamples, 0, length);

        return new AudioClip<TSample>(sample_rate, channels, format, layout, slicedSamples);
    }
}