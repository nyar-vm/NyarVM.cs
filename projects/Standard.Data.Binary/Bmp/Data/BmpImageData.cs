namespace Std.Data.Binary.Bmp.Data;

/// <summary>
///     BMP 图像文件数据的
/// </summary>
public sealed class BmpImageData
{
    /// <summary>
    ///     图像宽度（像素）的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     图像高度（像素），正值表示自底向上，负值表示自顶向下的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     每像素位数的
    /// </summary>
    public ushort bits_per_pixel { get; init; }

    /// <summary>
    ///     压缩方式的
    /// </summary>
    public BmpCompression compression { get; init; }

    /// <summary>
    ///     图像数据大小（字节）的
    /// </summary>
    public uint image_size { get; init; }

    /// <summary>
    ///     水平分辨率（像素/米）的
    /// </summary>
    public int x_pels_per_meter { get; init; }

    /// <summary>
    ///     垂直分辨率（像素/米）的
    /// </summary>
    public int y_pels_per_meter { get; init; }

    /// <summary>
    ///     使用的颜色数的
    /// </summary>
    public uint colors_used { get; init; }

    /// <summary>
    ///     重要的颜色数的
    /// </summary>
    public uint colors_important { get; init; }

    /// <summary>
    ///     调色板（的8 位及以下图像）的
    /// </summary>
    public uint[] palette { get; init; } = [];

    /// <summary>
    ///     像素数据的
    /// </summary>
    public byte[] pixel_data { get; init; } = [];

    /// <summary>
    ///     绝对高度（始终为正值）的
    /// </summary>
    public int absolute_height => System.Math.Abs(height);

    /// <summary>
    ///     是否为自顶向下存储的
    /// </summary>
    public bool is_top_down => height < 0;

    /// <summary>
    ///     每行字节数（的4 字节对齐填充）的
    /// </summary>
    public int stride => (width * bits_per_pixel + 31) / 32 * 4;
}