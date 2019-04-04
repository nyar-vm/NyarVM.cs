namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 分组元素（g）
///     。
/// </summary>
public sealed class SvgGroupElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.group;

    public override bool is_container => true;
}