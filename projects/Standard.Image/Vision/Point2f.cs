namespace Std.Image.Vision;

/// <summary>
///     二维浮点坐标结构体。
/// </summary>
public readonly struct Point2f
{
    /// <summary>
    ///     水平坐标。
    /// </summary>
    public float x { get; }

    /// <summary>
    ///     垂直坐标。
    /// </summary>
    public float y { get; }

    /// <summary>
    ///     初始化 <see cref="Point2f" /> 的新实例。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    public Point2f(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}