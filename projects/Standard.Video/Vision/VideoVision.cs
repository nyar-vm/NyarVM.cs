using Core.Media.Video;
using Std.Image.Pixels;

namespace Std.Video.Vision;

/// <summary>
///     视频视觉静态类，提供光流计算、目标跟踪和视频稳定等时域视觉能力。
/// </summary>
public static class VideoVision
{
    /// <summary>
    ///     计算两帧之间的光流场。
    /// </summary>
    /// <param name="previous">前一帧。</param>
    /// <param name="current">当前帧。</param>
    /// <returns>光流场。</returns>
    public static OpticalFlowField calc_optical_flow(IVideoFrame previous, IVideoFrame current)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     在视频片段中跟踪指定目标。
    /// </summary>
    /// <param name="clip">视频片段。</param>
    /// <param name="request">跟踪请求参数。</param>
    /// <returns>跟踪结果。</returns>
    public static TrackResult track(VideoClip<Rgba32> clip, TrackRequest request)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     对视频片段进行稳定化处理。
    /// </summary>
    /// <param name="clip">待稳定的视频片段。</param>
    /// <returns>稳定化后的视频片段。</returns>
    public static StabilizedClip stabilize(VideoClip<Rgba32> clip)
    {
        throw new NotImplementedException();
    }
}