namespace Std.Data.Binary.Tga.Data;

/// <summary>
///     TGA 图像文件数据，保的TGA 原生格式信息的
/// </summary>
public sealed class TgaImageData
{
    /// <summary>
    ///     图像宽度（像素）的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     图像高度（像素）的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     每像素位数（8/16/24/32）的
    /// </summary>
    public int pixel_depth { get; init; }

    /// <summary>
    ///     图像类型的
    /// </summary>
    public TgaImageType image_type { get; init; }

    /// <summary>
    ///     是否为从上到下的行序的
    /// </summary>
    public bool is_top_down { get; init; }

    /// <summary>
    ///     是否包含调色板的
    /// </summary>
    public bool has_color_map { get; init; }

    /// <summary>
    ///     调色板数据（RGBA 交错排列）的
    /// </summary>
    public byte[] color_map { get; init; } = [];

    /// <summary>
    ///     像素数据（TGA 原生 BGR/BGRA 顺序，未翻转）的
    /// </summary>
    public byte[] pixel_data { get; init; } = [];

    /// <summary>
    ///     图像 ID 长度的
    /// </summary>
    public int id_length { get; init; }

    /// <summary>
    ///     图像 ID 数据的
    /// </summary>
    public byte[] image_id { get; init; } = [];
}