namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 折线元素（polyline）
///     。
/// </summary>
public sealed class SvgPolylineElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.polyline;


    /// <summary>
    ///     顶点坐标列表（x0, y0, x1, y1, ...）
    ///     。
    /// </summary>
    public float[] points { get; init; } = [];
}