namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 遮罩元素（mask）
///     。
/// </summary>
public sealed class SvgMaskElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.mask;

    public override bool is_container => true;


    /// <summary>
    ///     遮罩单位
    ///     。
    /// </summary>
    public string mask_units { get; init; } = "objectBoundingBox";


    /// <summary>
    ///     遮罩内容单位
    ///     。
    /// </summary>
    public string mask_content_units { get; init; } = "userSpaceOnUse";
}