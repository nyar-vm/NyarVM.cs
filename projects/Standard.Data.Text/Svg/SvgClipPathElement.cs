namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 裁剪路径元素（clipPath）
///     。
/// </summary>
public sealed class SvgClipPathElement : SvgElement
{
    public override SvgElementType element_type => SvgElementType.clip_path;

    public override bool is_container => true;


    /// <summary>
    ///     裁剪单位
    ///     。
    /// </summary>
    public string clip_path_units { get; init; } = "userSpaceOnUse";
}