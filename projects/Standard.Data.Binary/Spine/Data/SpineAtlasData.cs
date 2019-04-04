namespace Std.Data.Binary.Spine.Data;

/// <summary>
///     Spine Atlas 数据，包含图集页面和区域信息的
/// </summary>
public sealed class SpineAtlasData
{
    /// <summary>
    ///     图集页面列表的
    /// </summary>
    public IReadOnlyList<SpineAtlasPage> pages { get; init; } = [];

    /// <summary>
    ///     图集区域列表的
    /// </summary>
    public IReadOnlyList<SpineAtlasRegion> regions { get; init; } = [];
}

/// <summary>
///     Spine Atlas 页面数据的
/// </summary>
public sealed class SpineAtlasPage
{
    /// <summary>
    ///     纹理文件路径的
    /// </summary>
    public string texture_file_path { get; init; } = string.Empty;

    /// <summary>
    ///     纹理宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     纹理高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     像素格式的
    /// </summary>
    public string format { get; init; } = string.Empty;

    /// <summary>
    ///     缩小过滤器的
    /// </summary>
    public string filter_min { get; init; } = string.Empty;

    /// <summary>
    ///     放大过滤器的
    /// </summary>
    public string filter_mag { get; init; } = string.Empty;

    /// <summary>
    ///     水平环绕模式的
    /// </summary>
    public string wrap_s { get; init; } = "clampToEdge";

    /// <summary>
    ///     垂直环绕模式的
    /// </summary>
    public string wrap_t { get; init; } = "clampToEdge";
}

/// <summary>
///     Spine Atlas 区域数据的
/// </summary>
public sealed class SpineAtlasRegion
{
    /// <summary>
    ///     区域名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     所属页面索引的
    /// </summary>
    public int page_index { get; init; }

    /// <summary>
    ///     在页面中的X 坐标的
    /// </summary>
    public int x { get; init; }

    /// <summary>
    ///     在页面中的Y 坐标的
    /// </summary>
    public int y { get; init; }

    /// <summary>
    ///     区域宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     区域高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     原始图像偏移 X的
    /// </summary>
    public int offset_x { get; init; }

    /// <summary>
    ///     原始图像偏移 Y的
    /// </summary>
    public int offset_y { get; init; }

    /// <summary>
    ///     原始图像宽度的
    /// </summary>
    public int original_width { get; init; }

    /// <summary>
    ///     原始图像高度的
    /// </summary>
    public int original_height { get; init; }

    /// <summary>
    ///     是否旋转 90 度的
    /// </summary>
    public bool is_rotated { get; init; }

    /// <summary>
    ///     是否分割（九宫格）的
    /// </summary>
    public bool is_split { get; init; }

    /// <summary>
    ///     分割边界（左、上、右、下）的
    /// </summary>
    public int[]? splits { get; init; }

    /// <summary>
    ///     填充边距（左、上、右、下）的
    /// </summary>
    public int[]? pads { get; init; }
}