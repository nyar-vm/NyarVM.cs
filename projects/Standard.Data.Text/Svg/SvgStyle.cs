namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 样式属性，包含填充、描边、透明度等视觉属性
/// </summary>
public sealed record SvgStyle
{
    /// <summary>
    ///     填充颜色（CSS 颜色值，如 "#FF0000"、"red"、"rgb(255,0,0)"、"none"）
    /// </summary>
    public string? fill { get; init; }


    /// <summary>
    ///     填充透明度（0.0 ~ 1.0）
    /// </summary>
    public float fill_opacity { get; init; } = 1f;


    /// <summary>
    ///     填充规则（"nonzero" 或 "evenodd"）
    ///     。
    /// </summary>
    public string fill_rule { get; init; } = "nonzero";


    /// <summary>
    ///     描边颜色
    ///     。
    /// </summary>
    public string? stroke { get; init; }


    /// <summary>
    ///     描边宽度
    ///     。
    /// </summary>
    public float stroke_width { get; init; }


    /// <summary>
    ///     描边透明度（0.0 ~ 1.0）
    ///     。
    /// </summary>
    public float stroke_opacity { get; init; } = 1f;


    /// <summary>
    ///     描边线帽（"butt"、"round"、"square"）
    ///     。
    /// </summary>
    public string stroke_linecap { get; init; } = "butt";


    /// <summary>
    ///     描边连接（"miter"、"round"、"bevel"）
    ///     。
    /// </summary>
    public string stroke_linejoin { get; init; } = "miter";


    /// <summary>
    ///     描边虚线偏移
    ///     。
    /// </summary>
    public float stroke_dashoffset { get; init; }


    /// <summary>
    ///     描边虚线模式（交替的线段长度和间隔长度）
    ///     。
    /// </summary>
    public float[] stroke_dasharray { get; init; } = [];


    /// <summary>
    ///     整体透明度（0.0 ~ 1.0）
    ///     。
    /// </summary>
    public float opacity { get; init; } = 1f;


    /// <summary>
    ///     是否无填充
    ///     。
    /// </summary>
    public bool is_fill_none => fill is "none" or null;


    /// <summary>
    ///     是否无描边
    ///     。
    /// </summary>
    public bool is_stroke_none => stroke is "none" or null;
}