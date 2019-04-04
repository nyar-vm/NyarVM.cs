using Core.Media;
using Std.Media;

namespace Std.Video.Processing;

/// <summary>
///     视频操作静态类，提供缩放、裁剪、拼接和像素格式转换等基础视频处理能力。
/// </summary>
public static class VideoOps
{
    /// <summary>
    ///     使用最近邻插值缩放视频帧到指定尺寸。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="frame">源视频帧。</param>
    /// <param name="width">目标宽度。</param>
    /// <param name="height">目标高度。</param>
    /// <returns>缩放后的视频帧。</returns>
    public static VideoFrame<TPixel> scale<TPixel>(VideoFrame<TPixel> frame, int width, int height)
        where TPixel : unmanaged
    {
        var result = new VideoFrame<TPixel>(width, height, frame.format, frame.timestamp);
        var sourceSpan = frame.readonly_span();
        var destSpan = result.span();

        for (var y = 0; y < height; y++)
        {
            var srcY = y * frame.height / height;

            for (var x = 0; x < width; x++)
            {
                var srcX = x * frame.width / width;
                destSpan[y * width + x] = sourceSpan[srcY * frame.width + srcX];
            }
        }

        return result;
    }

    /// <summary>
    ///     裁剪视频片段，返回指定时间范围内的子片段。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="clip">源视频片段。</param>
    /// <param name="startTicks">起始时间戳。</param>
    /// <param name="endTicks">结束时间戳。</param>
    /// <returns>裁剪后的视频片段。</returns>
    public static VideoClip<TPixel> trim<TPixel>(VideoClip<TPixel> clip, long startTicks, long endTicks)
        where TPixel : unmanaged
    {
        return clip.trim(startTicks, endTicks);
    }

    /// <summary>
    ///     拼接多个视频片段为一个连续片段。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="clips">要拼接的视频片段列表。</param>
    /// <returns>拼接后的视频片段。</returns>
    public static VideoClip<TPixel> concat<TPixel>(IReadOnlyList<VideoClip<TPixel>> clips)
        where TPixel : unmanaged
    {
        if (clips.Count == 0) throw new ArgumentException("拼接列表不能为空。");

        var first = clips[0];
        var result = new VideoClip<TPixel>(first.width, first.height, first.format, first.frame_rate);

        for (var i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];

            for (var j = 0; j < clip.frame_count; j++) result.append(clip.get_frame(j));
        }

        return result;
    }

    /// <summary>
    ///     转换视频帧的像素格式。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="frame">源视频帧。</param>
    /// <param name="targetFormat">目标像素格式。</param>
    /// <returns>转换后的视频帧。</returns>
    public static VideoFrame<TPixel> convert_pixel_format<TPixel>(VideoFrame<TPixel> frame, PixelFormat targetFormat)
        where TPixel : unmanaged
    {
        if (frame.format == targetFormat) return frame;

        var result = new VideoFrame<TPixel>(frame.width, frame.height, targetFormat, frame.timestamp);
        var sourceSpan = frame.readonly_span();
        var destSpan = result.span();

        sourceSpan.CopyTo(destSpan);

        return result;
    }
}