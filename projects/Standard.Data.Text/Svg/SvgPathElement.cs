namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 路径元素（path）
///     。
/// </summary>
public sealed class SvgPathElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.path;


    /// <summary>
    ///     路径数据命令列表
    ///     。
    /// </summary>
    public List<SvgPathCommand> commands { get; init; } = [];
}