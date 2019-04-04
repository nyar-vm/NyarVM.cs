namespace Plotter.Grammar;

/// <summary>
///     分面子图布局信息，描述子图在布局区域中的位置和尺寸。
/// </summary>
public readonly struct FacetLayout
{
    /// <summary>
    ///     子图左上角 X 坐标。
    /// </summary>
    public readonly float x;

    /// <summary>
    ///     子图左上角 Y 坐标。
    /// </summary>
    public readonly float y;

    /// <summary>
    ///     子图宽度。
    /// </summary>
    public readonly float width;

    /// <summary>
    ///     子图高度。
    /// </summary>
    public readonly float height;

    /// <summary>
    ///     初始化 <see cref="FacetLayout" /> 结构的新实例。
    /// </summary>
    /// <param name="x">左上角 X 坐标。</param>
    /// <param name="y">左上角 Y 坐标。</param>
    /// <param name="width">宽度。</param>
    /// <param name="height">高度。</param>
    public FacetLayout(float x, float y, float width, float height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
}