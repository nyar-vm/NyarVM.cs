namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 图像元素（image）
///     。
/// </summary>
public sealed class SvgImageElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.image;


    /// <summary>
    ///     图像源地址
    ///     。
    /// </summary>
    public string href { get; init; } = string.Empty;


    /// <summary>
    ///     左上角 X 坐标
    ///     。
    /// </summary>
    public float x { get; init; }


    /// <summary>
    ///     左上角 Y 坐标
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