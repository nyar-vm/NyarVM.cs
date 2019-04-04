namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 根元素
/// </summary>
public sealed class SvgRootElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.svg;

    public override bool is_container => true;


    /// <summary>
    ///     视图框（minX, minY, width, height）
    ///     。
    /// </summary>
    public float[] view_box { get; init; } = [];


    /// <summary>
    ///     文档宽度
    ///     。
    /// </summary>
    public float width { get; init; }


    /// <summary>
    ///     文档高度
    ///     。
    /// </summary>
    public float height { get; init; }


    /// <summary>
    ///     宽度单位
    ///     。
    /// </summary>
    public string width_unit { get; init; } = string.Empty;


    /// <summary>
    ///     高度单位
    ///     。
    /// </summary>
    public string height_unit { get; init; } = string.Empty;
}