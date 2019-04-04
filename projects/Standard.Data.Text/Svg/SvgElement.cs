namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 元素基类
///     。
/// </summary>
public abstract class SvgElement
{
    /// <summary>
    ///     元素类型
    ///     。
    /// </summary>
    public abstract SvgElementType element_type { get; }


    /// <summary>
    ///     元素 ID
    ///     。
    /// </summary>
    public string id { get; init; } = string.Empty;


    /// <summary>
    ///     CSS 类名
    ///     。
    /// </summary>
    public string @class { get; init; } = string.Empty;


    /// <summary>
    ///     变换列表
    ///     。
    /// </summary>
    public List<SvgTransform> transforms { get; init; } = [];


    /// <summary>
    ///     样式属性
    ///     。
    /// </summary>
    public SvgStyle style { get; init; } = new();


    /// <summary>
    ///     子元素列表
    ///     。
    /// </summary>
    public List<SvgElement> children { get; init; } = [];


    /// <summary>
    ///     是否为容器元素（可包含子元素）
    ///     。
    /// </summary>
    public virtual bool is_container => false;
}