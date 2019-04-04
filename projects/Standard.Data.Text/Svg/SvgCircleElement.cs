namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 圆形元素（circle）
///     。
/// </summary>
public sealed class SvgCircleElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.circle;


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
    ///     半径
    ///     。
    /// </summary>
    public float r { get; init; }
}