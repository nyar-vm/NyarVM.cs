using System;

namespace Core.Media.Image;

/// <summary>
///     标记图像类型，指定宽度、高度和像素格式。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class ImageAttribute : Attribute
{
    /// <summary>
    ///     初始化图像特性。
    /// </summary>
    /// <param name="width">图像宽度（像素）。</param>
    /// <param name="height">图像高度（像素）。</param>
    public ImageAttribute(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    /// <summary>
    ///     获取图像宽度（像素）。
    /// </summary>
    public int width { get; }

    /// <summary>
    ///     获取图像高度（像素）。
    /// </summary>
    public int height { get; }

    /// <summary>
    ///     获取或设置像素格式，默认为 <see cref="PixelFormat.rgba32" />。
    /// </summary>
    public PixelFormat pixel_format { get; init; } = PixelFormat.rgba32;
}