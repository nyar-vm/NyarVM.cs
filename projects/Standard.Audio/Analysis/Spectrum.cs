namespace Sonic.Audio.Analysis;

/// <summary>
///     频谱类，存储音频帧的频域分析结果，包含幅度和频率信息。
/// </summary>
public sealed class Spectrum
{
    /// <summary>
    ///     初始化 <see cref="Spectrum" /> 的新实例。
    /// </summary>
    /// <param name="magnitudes">频率幅度数组。</param>
    /// <param name="frequencies">频率值数组。</param>
    /// <param name="bin_count">频率仓数量。</param>
    /// <param name="duration">分析时长。</param>
    public Spectrum(float[] magnitudes, float[] frequencies, int bin_count, double duration)
    {
        this.magnitudes = magnitudes;
        this.frequencies = frequencies;
        this.bin_count = bin_count;
        this.duration = duration;
    }

    /// <summary>
    ///     频率幅度数组。
    /// </summary>
    public float[] magnitudes { get; }

    /// <summary>
    ///     频率值数组（Hz）。
    /// </summary>
    public float[] frequencies { get; }

    /// <summary>
    ///     频率仓数量。
    /// </summary>
    public int bin_count { get; }

    /// <summary>
    ///     分析时长（秒）。
    /// </summary>
    public double duration { get; }
}