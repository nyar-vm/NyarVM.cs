namespace Std.Image;

/// <summary>
///     图像元数据类，包含常见的图像描述信息。
/// </summary>
public sealed class ImageMetadata
{
    /// <summary>
    ///     初始化 <see cref="ImageMetadata" /> 的新实例。
    /// </summary>
    public ImageMetadata()
    {
        dpi_x = 96.0;
        dpi_y = 96.0;
        has_alpha = false;
        frame_count = 1;
        loop_count = 0;
        format_name = string.Empty;
        bit_depth = 8;
        color_space = string.Empty;
    }

    /// <summary>
    ///     水平分辨率（DPI）。
    /// </summary>
    public double dpi_x { get; set; }

    /// <summary>
    ///     垂直分辨率（DPI）。
    /// </summary>
    public double dpi_y { get; set; }

    /// <summary>
    ///     是否包含 Alpha 通道。
    /// </summary>
    public bool has_alpha { get; set; }

    /// <summary>
    ///     帧数（静态图像为 1）。
    /// </summary>
    public int frame_count { get; set; }

    /// <summary>
    ///     动画循环次数（0 表示无限循环）。
    /// </summary>
    public int loop_count { get; set; }

    /// <summary>
    ///     图像格式名称。
    /// </summary>
    public string format_name { get; set; }

    /// <summary>
    ///     位深度。
    /// </summary>
    public int bit_depth { get; set; }

    /// <summary>
    ///     色彩空间描述。
    /// </summary>
    public string color_space { get; set; }
}