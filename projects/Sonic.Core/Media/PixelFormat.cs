namespace Core.Media;

/// <summary>
///     像素格式枚举，定义图像和视频帧的像素排列方式。
/// </summary>
public enum PixelFormat
{
    /// <summary>
    ///     32 位 RGBA，每通道 8 位。
    /// </summary>
    rgba32,

    /// <summary>
    ///     32 位 BGRA，每通道 8 位。
    /// </summary>
    bgra32,

    /// <summary>
    ///     24 位 RGB，每通道 8 位。
    /// </summary>
    rgb24,

    /// <summary>
    ///     24 位 BGR，每通道 8 位。
    /// </summary>
    bgr24,

    /// <summary>
    ///     YUV 4:2:0 平面格式。
    /// </summary>
    yuv420_p,

    /// <summary>
    ///     YUV 4:2:2 平面格式。
    /// </summary>
    yuv422_p,

    /// <summary>
    ///     YUV 4:4:4 平面格式。
    /// </summary>
    yuv444_p,

    /// <summary>
    ///     8 位灰度。
    /// </summary>
    gray8,

    /// <summary>
    ///     16 位灰度。
    /// </summary>
    gray16,

    /// <summary>
    ///     64 位 RGBA，每通道 16 位。
    /// </summary>
    rgba64
}