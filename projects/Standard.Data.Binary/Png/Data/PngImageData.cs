namespace Std.Data.Binary.Png.Data;

/// <summary>
///     PNG 图像文件数据的
/// </summary>
public sealed class PngImageData
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
    ///     位深度的
    /// </summary>
    public byte bit_depth { get; init; }

    /// <summary>
    ///     色彩类型的
    /// </summary>
    public PngColorType color_type { get; init; }

    /// <summary>
    ///     压缩方法的
    /// </summary>
    public PngCompressionMethod compression_method { get; init; }

    /// <summary>
    ///     滤波方法的
    /// </summary>
    public PngFilterMethod filter_method { get; init; }

    /// <summary>
    ///     隔行扫描方法的
    /// </summary>
    public PngInterlaceMethod interlace_method { get; init; }

    /// <summary>
    ///     调色板（仅索引色图像）的
    /// </summary>
    public byte[] palette { get; init; } = [];

    /// <summary>
    ///     透明度数据（tRNS 块）的
    /// </summary>
    public byte[] transparency { get; init; } = [];

    /// <summary>
    ///     像素数据（已解压，含每行滤波器字节）的
    /// </summary>
    public byte[] raw_pixel_data { get; init; } = [];

    /// <summary>
    ///     辅助块列表的
    /// </summary>
    public IReadOnlyList<PngChunk> ancillary_chunks { get; init; } = [];

    /// <summary>
    ///     每像素字节数的
    /// </summary>
    public int bytes_per_pixel => color_type switch
    {
        PngColorType.grayscale => bit_depth <= 8 ? 1 : 2,
        PngColorType.truecolor => bit_depth <= 8 ? 3 : 6,
        PngColorType.indexed => 1,
        PngColorType.grayscale_alpha => bit_depth <= 8 ? 2 : 4,
        PngColorType.truecolor_alpha => bit_depth <= 8 ? 4 : 8,
        _ => 1
    };
}

/// <summary>
///     PNG 块数据的
/// </summary>
public sealed class PngChunk
{
    /// <summary>
    ///     块类型（4 字节 ASCII）的
    /// </summary>
    public string type { get; init; } = string.Empty;

    /// <summary>
    ///     块数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}