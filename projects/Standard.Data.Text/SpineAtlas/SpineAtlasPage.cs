namespace Std.Data.Text.SpineAtlas;

/// <summary>
///     Spine Atlas 页面定义
/// </summary>
public sealed class SpineAtlasPage
{
    /// <summary>
    ///     纹理文件路径
    /// </summary>
    public string texture_file_path { get; init; } = string.Empty;


    /// <summary>
    ///     纹理宽度
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     纹理高度
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     纹理格式
    /// </summary>
    public string format { get; init; } = string.Empty;


    /// <summary>
    ///     最小化过滤方式
    /// </summary>
    public string filter_min { get; init; } = string.Empty;


    /// <summary>
    ///     放大过滤方式
    /// </summary>
    public string filter_mag { get; init; } = string.Empty;


    /// <summary>
    ///     S 轴环绕方式
    /// </summary>
    public string wrap_s { get; init; } = "clampToEdge";


    /// <summary>
    ///     T 轴环绕方式
    /// </summary>
    public string wrap_t { get; init; } = "clampToEdge";
}