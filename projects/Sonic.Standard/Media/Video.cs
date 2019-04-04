using Core.Media;
using Core.Media.Video;

namespace Std.Media;

/// <summary>
///     视频帧结构体，提供视频帧的像素数据、维度、格式和时间戳信息。
/// </summary>
/// <typeparam name="TPixel">像素类型。</typeparam>
public readonly struct VideoFrame<TPixel> : IVideoFrame
    where TPixel : unmanaged
{
    /// <summary>
    ///     像素数据数组。
    /// </summary>
    public readonly TPixel[] pixels;

    /// <summary>
    ///     获取视频帧宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取视频帧高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     获取像素格式。
    /// </summary>
    public PixelFormat format { get; }

    /// <summary>
    ///     获取时间戳。
    /// </summary>
    public long timestamp { get; }

    /// <summary>
    ///     初始化 <see cref="VideoFrame{TPixel}" /> 的新实例。
    /// </summary>
    /// <param name="width">视频帧宽度。</param>
    /// <param name="height">视频帧高度。</param>
    /// <param name="format">像素格式。</param>
    /// <param name="timestamp">时间戳。</param>
    public VideoFrame(int width, int height, PixelFormat format, long timestamp)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        this.timestamp = timestamp;
        pixels = new TPixel[width * height];
    }

    /// <summary>
    ///     初始化 <see cref="VideoFrame{TPixel}" /> 的新实例，使用已有像素数据。
    /// </summary>
    /// <param name="width">视频帧宽度。</param>
    /// <param name="height">视频帧高度。</param>
    /// <param name="format">像素格式。</param>
    /// <param name="timestamp">时间戳。</param>
    /// <param name="pixels">像素数据。</param>
    public VideoFrame(int width, int height, PixelFormat format, long timestamp, TPixel[] pixels)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        this.timestamp = timestamp;
        this.pixels = pixels;
    }

    /// <summary>
    ///     获取或设置指定位置的像素。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    /// <returns>指定位置的像素值。</returns>
    public ref TPixel this[int x, int y] => ref pixels[y * width + x];

    /// <summary>
    ///     获取像素数据的可写跨度。
    /// </summary>
    /// <returns>像素数据跨度。</returns>
    public Span<TPixel> span()
    {
        return pixels.AsSpan();
    }

    /// <summary>
    ///     获取像素数据的只读跨度。
    /// </summary>
    /// <returns>像素数据只读跨度。</returns>
    public ReadOnlySpan<TPixel> readonly_span()
    {
        return pixels.AsSpan();
    }
}