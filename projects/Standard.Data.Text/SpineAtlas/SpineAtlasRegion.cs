namespace Std.Data.Text.SpineAtlas;

/// <summary>
///     Spine Atlas 区域定义
/// </summary>
public sealed record SpineAtlasRegion
{
    /// <summary>
    ///     区域名称
    /// </summary>
    public string name { get; init; } = string.Empty;


    /// <summary>
    ///     所属页面索引
    /// </summary>
    public int page_index { get; init; }


    /// <summary>
    ///     X 坐标
    /// </summary>
    public int x { get; init; }


    /// <summary>
    ///     Y 坐标
    /// </summary>
    public int y { get; init; }


    /// <summary>
    ///     宽度
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     高度
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     X 偏移量
    /// </summary>
    public int offset_x { get; init; }


    /// <summary>
    ///     Y 偏移量
    /// </summary>
    public int offset_y { get; init; }


    /// <summary>
    ///     原始宽度
    /// </summary>
    public int original_width { get; init; }


    /// <summary>
    ///     原始高度
    /// </summary>
    public int original_height { get; init; }


    /// <summary>
    ///     是否旋转 90 度
    /// </summary>
    public bool is_rotated { get; init; }


    /// <summary>
    ///     是否分割（九宫格）
    /// </summary>
    public bool is_split { get; init; }


    /// <summary>
    ///     分割数据 [left, right, top, bottom]
    /// </summary>
    public int[]? splits { get; init; }


    /// <summary>
    ///     填充数据 [left, right, top, bottom]
    /// </summary>
    public int[]? pads { get; init; }
}