namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 线段元素（line）
///     。
/// </summary>
public sealed class SvgLineElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.line;


    /// <summary>
    ///     起点 X 坐标
    ///     。
    /// </summary>
    public float x1 { get; init; }


    /// <summary>
    ///     起点 Y 坐标
    ///     。
    /// </summary>
    public float y1 { get; init; }


    /// <summary>
    ///     终点 X 坐标
    ///     。
    /// </summary>
    public float x2 { get; init; }


    /// <summary>
    ///     终点 Y 坐标
    ///     。
    /// </summary>
    public float y2 { get; init; }
}