namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 文本元素（text）
///     。
/// </summary>
public sealed class SvgTextElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.text;


    /// <summary>
    ///     文本基线 X 坐标
    ///     。
    /// </summary>
    public float x { get; init; }


    /// <summary>
    ///     文本基线 Y 坐标
    ///     。
    /// </summary>
    public float y { get; init; }


    /// <summary>
    ///     文本内容
    ///     。
    /// </summary>
    public string text { get; init; } = string.Empty;


    /// <summary>
    ///     字体族
    ///     。
    /// </summary>
    public string font_family { get; init; } = string.Empty;


    /// <summary>
    ///     字体大小
    ///     。
    /// </summary>
    public float font_size { get; init; } = 16f;


    /// <summary>
    ///     文本锚点（"start"、"middle"、"end"）
    ///     。
    /// </summary>
    public string text_anchor { get; init; } = "start";
}