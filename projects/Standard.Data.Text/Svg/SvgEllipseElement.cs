namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 椭圆元素（ellipse）
///     。
/// </summary>
public sealed class SvgEllipseElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.ellipse;


    /// <summary>
    ///     圆心 X 坐标
    ///     。
    /// </summary>
    public float cx { get; init; }


    /// <summary>
    ///     圆心 Y 坐标
    ///     。
    /// </summary>
    public float cy { get; init; }


    /// <summary>
    ///     水平半径
    ///     。
    /// </summary>
    public float rx { get; init; }


    /// <summary>
    ///     垂直半径
    ///     。
    /// </summary>
    public float ry { get; init; }
}