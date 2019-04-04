using Core.Media;
using Core.Media.Image;

namespace Std.Media;

/// <summary>
///     通用图像结构体，提供像素级图像数据的存储与操作能力。
/// </summary>
/// <typeparam name="TPixel">像素类型。</typeparam>
public readonly struct Image<TPixel> : IImage
    where TPixel : unmanaged
{
    /// <summary>
    ///     像素数据数组。
    /// </summary>
    public readonly TPixel[] pixels;

    /// <summary>
    ///     获取图像宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取图像高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     获取像素格式。
    /// </summary>
    public PixelFormat format { get; }

    /// <summary>
    ///     初始化 <see cref="Image{TPixel}" /> 的新实例。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="format">像素格式。</param>
    public Image(int width, int height, PixelFormat format)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        pixels = new TPixel[width * height];
    }

    /// <summary>
    ///     初始化 <see cref="Image{TPixel}" /> 的新实例，使用已有像素数据。
    /// </summary>
    /// <param name="width">图像宽度。</param>
    /// <param name="height">图像高度。</param>
    /// <param name="format">像素格式。</param>
    /// <param name="pixels">像素数据。</param>
    public Image(int width, int height, PixelFormat format, TPixel[] pixels)
    {
        this.width = width;
        this.height = height;
        this.format = format;
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
    ///     调整图像大小，返回新的图像实例。
    /// </summary>
    /// <param name="new_width">新宽度。</param>
    /// <param name="new_height">新高度。</param>
    /// <returns>调整大小后的新图像。</returns>
    public Image<TPixel> resize(int newWidth, int newHeight)
    {
        var result = new Image<TPixel>(newWidth, newHeight, format);

        var copyWidth = System.Math.Min(width, newWidth);
        var copyHeight = System.Math.Min(height, newHeight);

        for (var y = 0; y < copyHeight; y++)
        for (var x = 0; x < copyWidth; x++)
            result[x, y] = this[x, y];

        return result;
    }

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

    /// <summary>
    ///     裁剪图像，返回指定区域的新图像实例。
    /// </summary>
    /// <param name="x">裁剪区域起始水平坐标。</param>
    /// <param name="y">裁剪区域起始垂直坐标。</param>
    /// <param name="crop_width">裁剪区域宽度。</param>
    /// <param name="crop_height">裁剪区域高度。</param>
    /// <returns>裁剪后的新图像。</returns>
    public Image<TPixel> crop(int x, int y, int cropWidth, int cropHeight)
    {
        var result = new Image<TPixel>(cropWidth, cropHeight, format);

        for (var dy = 0; dy < cropHeight; dy++)
        for (var dx = 0; dx < cropWidth; dx++)
        {
            var srcX = x + dx;
            var srcY = y + dy;

            if (srcX >= 0 && srcX < width && srcY >= 0 && srcY < height) result[dx, dy] = this[srcX, srcY];
        }

        return result;
    }
}