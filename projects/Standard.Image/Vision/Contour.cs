namespace Std.Image.Vision;

/// <summary>
///     轮廓类，表示图像中检测到的连通区域轮廓。
/// </summary>
public sealed class Contour
{
    /// <summary>
    ///     初始化 <see cref="Contour" /> 的新实例。
    /// </summary>
    /// <param name="points">轮廓点集。</param>
    /// <param name="area">包围面积。</param>
    /// <param name="perimeter">轮廓周长。</param>
    public Contour(Point2f[] points, double area, double perimeter)
    {
        this.points = points;
        this.area = area;
        this.perimeter = perimeter;
    }

    /// <summary>
    ///     轮廓点集。
    /// </summary>
    public Point2f[] points { get; }

    /// <summary>
    ///     轮廓包围面积。
    /// </summary>
    public double area { get; }

    /// <summary>
    ///     轮廓周长。
    /// </summary>
    public double perimeter { get; }
}