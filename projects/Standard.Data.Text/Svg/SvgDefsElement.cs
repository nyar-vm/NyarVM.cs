namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 定义元素（defs）
///     。
/// </summary>
public sealed class SvgDefsElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.defs;

    public override bool is_container => true;
}