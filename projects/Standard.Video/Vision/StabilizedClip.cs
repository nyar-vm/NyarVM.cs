using Std.Image.Pixels;

namespace Std.Video.Vision;

/// <summary>
///     稳定化视频片段类，包含稳定后的视频片段和变换数据。
/// </summary>
public sealed class StabilizedClip
{
    /// <summary>
    ///     初始化 <see cref="StabilizedClip" /> 的新实例。
    /// </summary>
    /// <param name="clip">稳定后的视频片段。</param>
    /// <param name="transformData">帧间变换数据。</param>
    /// <param name="stabilityScore">稳定度评分。</param>
    public StabilizedClip(VideoClip<Rgba32> clip, float[] transformData, double stabilityScore)
    {
        this.clip = clip;
        transform_data = transformData;
        stability_score = stabilityScore;
    }

    /// <summary>
    ///     获取稳定后的视频片段。
    /// </summary>
    public VideoClip<Rgba32> clip { get; }

    /// <summary>
    ///     获取帧间变换数据，每帧包含仿射变换参数。
    /// </summary>
    public float[] transform_data { get; }

    /// <summary>
    ///     获取稳定度评分，值越高表示越稳定。
    /// </summary>
    public double stability_score { get; }
}