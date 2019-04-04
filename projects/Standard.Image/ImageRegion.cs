namespace Std.Image;

/// <summary>
///     图像区域结构体，表示一个矩形子区域。
/// </summary>
public readonly struct ImageRegion
{
    /// <summary>
    ///     区域起始水平坐标。
    /// </summary>
    public int x { get; }

    /// <summary>
    ///     区域起始垂直坐标。
    /// </summary>
    public int y { get; }

    /// <summary>
    ///     区域宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     区域高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     初始化 <see cref="ImageRegion" /> 的新实例。
    /// </summary>
    /// <param name="x">起始水平坐标。</param>
    /// <param name="y">起始垂直坐标。</param>
    /// <param name="width">区域宽度。</param>
    /// <param name="height">区域高度。</param>
    public ImageRegion(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    ///     获取区域的总像素数。
    /// </summary>
    public int pixel_count => width * height;
}