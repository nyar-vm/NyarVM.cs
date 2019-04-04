namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 线性渐变元素（linearGradient）
///     。
/// </summary>
public sealed class SvgLinearGradientElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.linear_gradient;

    public override bool is_container => true;


    /// <summary>
    ///     渐变起点 X（0~1 或绝对值）
    ///     。
    /// </summary>
    public float x1 { get; init; }


    /// <summary>
    ///     渐变起点 Y
    ///     。
    /// </summary>
    public float y1 { get; init; }


    /// <summary>
    ///     渐变终点 X
    ///     。
    /// </summary>
    public float x2 { get; init; } = 1f;


    /// <summary>
    ///     渐变终点 Y
    ///     。
    /// </summary>
    public float y2 { get; init; }


    /// <summary>
    ///     渐变单位（"userSpaceOnUse" 或 "objectBoundingBox"）
    ///     。
    /// </summary>
    public string gradient_units { get; init; } = "objectBoundingBox";
}