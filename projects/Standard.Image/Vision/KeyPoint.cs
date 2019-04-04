namespace Std.Image.Vision;

/// <summary>
///     关键点结构体，表示图像特征检测中的特征点。
/// </summary>
public readonly struct KeyPoint
{
    /// <summary>
    ///     关键点水平坐标。
    /// </summary>
    public float x { get; }

    /// <summary>
    ///     关键点垂直坐标。
    /// </summary>
    public float y { get; }

    /// <summary>
    ///     关键点尺寸（直径）。
    /// </summary>
    public float size { get; }

    /// <summary>
    ///     关键点方向角度（度）。
    /// </summary>
    public float angle { get; }

    /// <summary>
    ///     关键点响应强度。
    /// </summary>
    public float response { get; }

    /// <summary>
    ///     初始化 <see cref="KeyPoint" /> 的新实例。
    /// </summary>
    /// <param name="x">水平坐标。</param>
    /// <param name="y">垂直坐标。</param>
    /// <param name="size">关键点尺寸。</param>
    /// <param name="angle">方向角度。</param>
    /// <param name="response">响应强度。</param>
    public KeyPoint(float x, float y, float size, float angle, float response)
    {
        this.x = x;
        this.y = y;
        this.size = size;
        this.angle = angle;
        this.response = response;
    }
}