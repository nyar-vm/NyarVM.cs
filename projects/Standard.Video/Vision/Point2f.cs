namespace Std.Video.Vision;

/// <summary>
///     二维浮点坐标结构体，表示图像或视频帧中的点位置。
/// </summary>
public readonly struct Point2F
{
    /// <summary>
    ///     水平坐标。
    /// </summary>
    public readonly float x;

    /// <summary>
    ///     垂直坐标。
    /// </summary>
    public readonly float y;

    /// <summary>
    ///     初始化 <see cref="Point2F" /> 的新实例。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    public Point2F(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}