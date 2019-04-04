namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 未知元素（用于保留不支持的元素）
///     。
/// </summary>
public sealed class SvgUnknownElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.unknown;


    /// <summary>
    ///     原始元素名称
    ///     。
    /// </summary>
    public string original_name { get; init; } = string.Empty;


    /// <summary>
    ///     原始属性列表
    ///     。
    /// </summary>
    public List<(string Name, string Value)> raw_attributes { get; init; } = [];
}