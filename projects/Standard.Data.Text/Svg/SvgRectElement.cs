namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 矩形元素（rect）
///     。
/// </summary>
public sealed class SvgRectElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.rect;


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


    /// <summary>
    ///     水平圆角半径
    ///     。
    /// </summary>
    public float rx { get; init; }


    /// <summary>
    ///     垂直圆角半径
    ///     。
    /// </summary>
    public float ry { get; init; }


    /// <summary>
    ///     是否有圆角
    ///     。
    /// </summary>
    public bool has_rounded_corners => rx > 0 || ry > 0;
}