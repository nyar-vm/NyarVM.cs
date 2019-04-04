using System;

namespace Core.Media.Video;

/// <summary>
///     标记视频帧类型，指定宽度、高度、像素格式和时间戳。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class VideoFrameAttribute : Attribute
{
    /// <summary>
    ///     初始化视频帧特性。
    /// </summary>
    /// <param name="width">视频帧宽度（像素）。</param>
    /// <param name="height">视频帧高度（像素）。</param>
    public VideoFrameAttribute(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    ///     获取视频帧宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取视频帧高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     获取或设置像素格式，默认为 <see cref="Media.PixelFormat.rgba32" />。
    /// </summary>
    public PixelFormat pixel_format { get; init; } = PixelFormat.rgba32;

    /// <summary>
    ///     获取或设置时间戳，默认为 0。
    /// </summary>
    public long timestamp { get; init; }
}