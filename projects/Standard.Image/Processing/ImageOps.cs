using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Std.Media;

namespace Std.Image.Processing;

/// <summary>
///     图像处理操作静态类，提供裁剪、缩放、翻转和像素格式转换等基础操作。
/// </summary>
public static class ImageOps
{
    /// <summary>
    ///     裁剪图像，返回指定区域的新图像实例。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="image">源图像。</param>
    /// <param name="region">裁剪区域。</param>
    /// <returns>裁剪后的新图像。</returns>
    public static Image<TPixel> crop<TPixel>(Image<TPixel> image, ImageRegion region)
        where TPixel : unmanaged
    {
        var result = new Image<TPixel>(region.width, region.height, image.format);

        for (var dy = 0; dy < region.height; dy++)
        for (var dx = 0; dx < region.width; dx++)
        {
            var srcX = region.x + dx;
            var srcY = region.y + dy;

            if (srcX >= 0 && srcX < image.width && srcY >= 0 && srcY < image.height) result[dx, dy] = image[srcX, srcY];
        }

        return result;
    }

    /// <summary>
    ///     使用最近邻插值调整图像大小。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="image">源图像。</param>
    /// <param name="width">目标宽度。</param>
    /// <param name="height">目标高度。</param>
    /// <returns>调整大小后的新图像。</returns>
    public static Image<TPixel> resize_nearest<TPixel>(Image<TPixel> image, int width, int height)
        where TPixel : unmanaged
    {
        var result = new Image<TPixel>(width, height, image.format);

        var xRatio = (double)image.width / width;
        var yRatio = (double)image.height / height;

        for (var y = 0; y < height; y++)
        {
            var srcY = (int)(y * yRatio);

            for (var x = 0; x < width; x++)
            {
                var srcX = (int)(x * xRatio);
                result[x, y] = image[srcX, srcY];
            }
        }

        return result;
    }

    /// <summary>
    ///     水平翻转图像。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="image">源图像。</param>
    /// <returns>水平翻转后的新图像。</returns>
    public static Image<TPixel> flip_horizontal<TPixel>(Image<TPixel> image)
        where TPixel : unmanaged
    {
        var result = new Image<TPixel>(image.width, image.height, image.format);

        for (var y = 0; y < image.height; y++)
        for (var x = 0; x < image.width; x++)
            result[x, y] = image[image.width - 1 - x, y];

        return result;
    }

    /// <summary>
    ///     垂直翻转图像。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="image">源图像。</param>
    /// <returns>垂直翻转后的新图像。</returns>
    public static Image<TPixel> flip_vertical<TPixel>(Image<TPixel> image)
        where TPixel : unmanaged
    {
        var result = new Image<TPixel>(image.width, image.height, image.format);

        for (var y = 0; y < image.height; y++)
        for (var x = 0; x < image.width; x++)
            result[x, y] = image[x, image.height - 1 - y];

        return result;
    }

    /// <summary>
    ///     逐像素重新解释转换图像的像素格式。
    /// </summary>
    /// <typeparam name="TSource">源像素类型。</typeparam>
    /// <typeparam name="TTarget">目标像素类型。</typeparam>
    /// <param name="image">源图像。</param>
    /// <returns>转换后的新图像。</returns>
    /// <remarks>
    ///     此方法执行内存级别的重新解释，要求源像素类型和目标像素类型的大小一致。
    /// </remarks>
    public static Image<TTarget> convert<TSource, TTarget>(Image<TSource> image)
        where TSource : unmanaged
        where TTarget : unmanaged
    {
        if (Unsafe.SizeOf<TSource>() != Unsafe.SizeOf<TTarget>())
            throw new InvalidOperationException(
                $"源像素类型大小（{Unsafe.SizeOf<TSource>()} 字节）与目标像素类型大小（{Unsafe.SizeOf<TTarget>()} 字节）不一致，无法进行重新解释转换。");

        var sourceSpan = image.span();
        var targetPixels = new TTarget[image.width * image.height];
        var targetSpan = targetPixels.AsSpan();

        sourceSpan.CopyTo(MemoryMarshal.Cast<TTarget, TSource>(targetSpan));

        return new Image<TTarget>(image.width, image.height, image.format, targetPixels);
    }
}