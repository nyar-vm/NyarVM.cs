namespace Sonic.Audio.Analysis;

/// <summary>
///     节拍追踪类，存储音频节拍检测的结果，包含时间戳和强度信息。
/// </summary>
public sealed class BeatTrack
{
    /// <summary>
    ///     初始化 <see cref="BeatTrack" /> 的新实例。
    /// </summary>
    /// <param name="timestamps">节拍时间戳数组。</param>
    /// <param name="strengths">节拍强度数组。</param>
    /// <param name="beat_count">节拍数量。</param>
    public BeatTrack(double[] timestamps, double[] strengths, int beat_count)
    {
        this.timestamps = timestamps;
        this.strengths = strengths;
        this.beat_count = beat_count;
    }

    /// <summary>
    ///     节拍时间戳数组（秒）。
    /// </summary>
    public double[] timestamps { get; }

    /// <summary>
    ///     节拍强度数组。
    /// </summary>
    public double[] strengths { get; }

    /// <summary>
    ///     节拍数量。
    /// </summary>
    public int beat_count { get; }
}