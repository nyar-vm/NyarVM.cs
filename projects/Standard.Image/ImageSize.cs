namespace Std.Image;

/// <summary>
///     图像尺寸结构体，表示宽度和高度。
/// </summary>
public readonly struct ImageSize
{
    /// <summary>
    ///     图像宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     图像高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     初始化 <see cref="ImageSize" /> 的新实例。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    public ImageSize(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    ///     获取总像素数。
    /// </summary>
    public int pixel_count => width * height;
}