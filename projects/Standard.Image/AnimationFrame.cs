using Std.Media;

namespace Std.Image;

/// <summary>
///     动画帧结构体，表示动画图像中的单帧。
/// </summary>
/// <typeparam name="TPixel">像素类型。</typeparam>
public readonly struct AnimationFrame<TPixel>
    where TPixel : unmanaged
{
    /// <summary>
    ///     帧图像数据。
    /// </summary>
    public Image<TPixel> image { get; }

    /// <summary>
    ///     帧延迟时间（毫秒）。
    /// </summary>
    public int delay_ms { get; }

    /// <summary>
    ///     帧混合区域，为 null 时表示整帧混合。
    /// </summary>
    public ImageRegion? blend_region { get; }

    /// <summary>
    ///     初始化 <see cref="AnimationFrame{TPixel}" /> 的新实例。
    /// </summary>
    /// <param name="image">帧图像数据。</param>
    /// <param name="delay_ms">帧延迟时间（毫秒）。</param>
    /// <param name="blend_region">帧混合区域。</param>
    public AnimationFrame(Image<TPixel> image, int delay_ms, ImageRegion? blend_region = null)
    {
        this.image = image;
        this.delay_ms = delay_ms;
        this.blend_region = blend_region;
    }
}