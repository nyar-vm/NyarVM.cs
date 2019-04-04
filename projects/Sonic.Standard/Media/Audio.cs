using Core.Media;
using Core.Media.Audio;

namespace Std.Media;

/// <summary>
///     音频缓冲区结构体，提供音频采样数据的存储与基本参数信息。
/// </summary>
/// <typeparam name="T">采样数据类型。</typeparam>
public readonly struct AudioBuffer<T> : IAudio
    where T : unmanaged
{
    /// <summary>
    ///     采样数据数组。
    /// </summary>
    public readonly T[] samples;

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
    ///     获取音频时长（秒）。
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
    ///     初始化 <see cref="AudioBuffer{T}" /> 的新实例。
    /// </summary>
    /// <param name="sample_rate">采样率。</param>
    /// <param name="channels">声道数。</param>
    /// <param name="format">采样格式。</param>
    public AudioBuffer(int sampleRate, int channels, SampleFormat format)
    {
        sample_rate = sampleRate;
        this.channels = channels;
        this.format = format;
        layout = channels switch
        {
            1 => ChannelLayout.mono,
            2 => ChannelLayout.stereo,
            6 => ChannelLayout.surround5_1,
            8 => ChannelLayout.surround7_1,
            _ => ChannelLayout.mono
        };
        samples = [];
    }

    /// <summary>
    ///     初始化 <see cref="AudioBuffer{T}" /> 的新实例，使用已有采样数据。
    /// </summary>
    /// <param name="sample_rate">采样率。</param>
    /// <param name="channels">声道数。</param>
    /// <param name="format">采样格式。</param>
    /// <param name="samples">采样数据。</param>
    public AudioBuffer(int sampleRate, int channels, SampleFormat format, T[] samples)
    {
        sample_rate = sampleRate;
        this.channels = channels;
        this.format = format;
        layout = channels switch
        {
            1 => ChannelLayout.mono,
            2 => ChannelLayout.stereo,
            6 => ChannelLayout.surround5_1,
            8 => ChannelLayout.surround7_1,
            _ => ChannelLayout.mono
        };
        this.samples = samples;
    }

    /// <summary>
    ///     初始化 <see cref="AudioBuffer{T}" /> 的新实例，指定声道布局。
    /// </summary>
    /// <param name="sample_rate">采样率。</param>
    /// <param name="channels">声道数。</param>
    /// <param name="format">采样格式。</param>
    /// <param name="layout">声道布局。</param>
    /// <param name="samples">采样数据。</param>
    public AudioBuffer(int sampleRate, int channels, SampleFormat format, ChannelLayout layout, T[] samples)
    {
        sample_rate = sampleRate;
        this.channels = channels;
        this.format = format;
        this.layout = layout;
        this.samples = samples;
    }

    /// <summary>
    ///     获取指定索引处的采样值。
    /// </summary>
    /// <param name="index">采样索引。</param>
    /// <returns>采样值。</returns>
    public ref T this[int index] => ref samples[index];

    /// <summary>
    ///     获取采样数据的可写跨度。
    /// </summary>
    /// <returns>采样数据跨度。</returns>
    public Span<T> span()
    {
        return samples.AsSpan();
    }

    /// <summary>
    ///     获取采样数据的只读跨度。
    /// </summary>
    /// <returns>采样数据只读跨度。</returns>
    public ReadOnlySpan<T> readonly_span()
    {
        return samples.AsSpan();
    }
}