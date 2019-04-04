namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 引用元素（use）
///     。
/// </summary>
public sealed class SvgUseElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.use;


    /// <summary>
    ///     引用目标（href 或 xlink:href）
    ///     。
    /// </summary>
    public string href { get; init; } = string.Empty;


    /// <summary>
    ///     X 偏移
    ///     。
    /// </summary>
    public float x { get; init; }


    /// <summary>
    ///     Y 偏移
    ///     。
    /// </summary>
    public float y { get; init; }


    /// <summary>
    ///     宽度
    ///     。
    /// </summary>
    public float width { get; init; }


    /// <summary>
    ///     高度
    ///     。
    /// </summary>
    public float height { get; init; }
}