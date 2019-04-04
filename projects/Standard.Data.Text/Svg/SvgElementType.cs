namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 元素类型
/// </summary>
public enum SvgElementType : byte
{
    /// <summary>
    ///     SVG 根元素
    /// </summary>
    svg = 0,

    /// <summary>
    ///     分组元素
    /// </summary>
    group = 1,

    /// <summary>
    ///     路径元素
    /// </summary>
    path = 2,

    /// <summary>
    ///     矩形元素
    /// </summary>
    rect = 3,

    /// <summary>
    ///     圆形元素
    /// </summary>
    circle = 4,

    /// <summary>
    ///     椭圆元素
    /// </summary>
    ellipse = 5,

    /// <summary>
    ///     线段元素
    /// </summary>
    line = 6,

    /// <summary>
    ///     折线元素
    /// </summary>
    polyline = 7,

    /// <summary>
    ///     多边形元素
    /// </summary>
    polygon = 8,

    /// <summary>
    ///     文本元素
    /// </summary>
    text = 9,

    /// <summary>
    ///     定义元素
    /// </summary>
    defs = 10,

    /// <summary>
    ///     引用元素
    /// </summary>
    use = 11,

    /// <summary>
    ///     图像元素
    /// </summary>
    image = 12,

    /// <summary>
    ///     线性渐变
    /// </summary>
    linear_gradient = 13,

    /// <summary>
    ///     径向渐变
    /// </summary>
    radial_gradient = 14,

    /// <summary>
    ///     裁剪路径
    /// </summary>
    clip_path = 15,

    /// <summary>
    ///     遮罩
    /// </summary>
    mask = 16,

    /// <summary>
    ///     未知元素
    /// </summary>
    unknown = 255
}