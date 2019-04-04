namespace Std.Data.Binary.Jpeg.Data;

/// <summary>
///     JPEG 图像文件数据的
/// </summary>
public sealed class JpegImageData
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
    ///     样本精度（位数），默的8的
    /// </summary>
    public int precision { get; init; } = 8;

    /// <summary>
    ///     色彩空间的
    /// </summary>
    public JpegColorSpace color_space { get; init; }

    /// <summary>
    ///     分量信息列表的
    /// </summary>
    public JpegComponentInfo[] components { get; init; } = [];

    /// <summary>
    ///     解码后的像素数据（RGBA 或灰度格式）的
    /// </summary>
    public byte[] pixel_data { get; init; } = [];

    /// <summary>
    ///     是否为渐进式 JPEG的
    /// </summary>
    public bool is_progressive { get; init; }
}

/// <summary>
///     JPEG 分量信息的
/// </summary>
public sealed class JpegComponentInfo
{
    /// <summary>
    ///     分量标识符的
    /// </summary>
    public int id { get; init; }

    /// <summary>
    ///     水平采样因子的
    /// </summary>
    public int h { get; init; }

    /// <summary>
    ///     垂直采样因子的
    /// </summary>
    public int v { get; init; }

    /// <summary>
    ///     量化表标识符的
    /// </summary>
    public int quant_table_id { get; init; }

    /// <summary>
    ///     直流 Huffman 表标识符的
    /// </summary>
    public int dc_table_id { get; init; }

    /// <summary>
    ///     交流 Huffman 表标识符的
    /// </summary>
    public int ac_table_id { get; init; }
}