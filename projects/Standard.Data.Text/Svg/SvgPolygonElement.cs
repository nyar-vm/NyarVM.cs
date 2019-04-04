namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 多边形元素（polygon）
///     。
/// </summary>
public sealed class SvgPolygonElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.polygon;


    /// <summary>
    ///     顶点坐标列表（x0, y0, x1, y1, ...）
    ///     。
    /// </summary>
    public float[] points { get; init; } = [];
}