namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 径向渐变元素（radialGradient）
///     。
/// </summary>
public sealed class SvgRadialGradientElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.radial_gradient;

    public override bool is_container => true;


    /// <summary>
    ///     圆心 X
    ///     。
    /// </summary>
    public float cx { get; init; } = 0.5f;


    /// <summary>
    ///     圆心 Y
    ///     。
    /// </summary>
    public float cy { get; init; } = 0.5f;


    /// <summary>
    ///     半径
    ///     。
    /// </summary>
    public float r { get; init; } = 0.5f;


    /// <summary>
    ///     焦点 X
    ///     。
    /// </summary>
    public float fx { get; init; }


    /// <summary>
    ///     焦点 Y
    ///     。
    /// </summary>
    public float fy { get; init; }


    /// <summary>
    ///     渐变单位
    ///     。
    /// </summary>
    public string gradient_units { get; init; } = "objectBoundingBox";
}