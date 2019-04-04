using Core.Media;

namespace Std.Video;

/// <summary>
///     视频编码选项类，指定编码器参数。
/// </summary>
public sealed class VideoEncodeOptions
{
    /// <summary>
    ///     获取或设置编解码器名称。
    /// </summary>
    public string codec_name { get; set; } = "";

    /// <summary>
    ///     获取或设置比特率。
    /// </summary>
    public int bit_rate { get; set; }

    /// <summary>
    ///     获取或设置帧率。
    /// </summary>
    public double frame_rate { get; set; }

    /// <summary>
    ///     获取或设置像素格式。
    /// </summary>
    public PixelFormat pixel_format { get; set; }

    /// <summary>
    ///     获取或设置视频宽度。
    /// </summary>
    public int width { get; set; }

    /// <summary>
    ///     获取或设置视频高度。
    /// </summary>
    public int height { get; set; }
}