using Std.Media;

namespace Std.Image.Vision;

/// <summary>
///     传统计算机视觉操作静态类，提供边缘检测、轮廓查找和特征点检测等算法。
/// </summary>
public static class VisionOps
{
    /// <summary>
    ///     使用 Canny 算法进行边缘检测。
    /// </summary>
    /// <param name="image">输入灰度图像。</param>
    /// <param name="low">低阈值。</param>
    /// <param name="high">高阈值。</param>
    /// <returns>边缘图像。</returns>
    public static Image<byte> canny(Image<byte> image, double low, double high)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     查找图像中的轮廓。
    /// </summary>
    /// <param name="image">输入二值化图像。</param>
    /// <returns>检测到的轮廓数组。</returns>
    public static Contour[] find_contours(Image<byte> image)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     使用 ORB 算法检测特征点。
    /// </summary>
    /// <param name="image">输入灰度图像。</param>
    /// <returns>检测到的关键点数组。</returns>
    public static KeyPoint[] detect_orb(Image<byte> image)
    {
        throw new NotImplementedException();
    }
}