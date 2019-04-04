namespace Std.Data.Binary.Exr.Data;

/// <summary>
///     OpenEXR 图像数据的
/// </summary>
public sealed class ExrImageData
{
    /// <summary>
    ///     图像宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     图像高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     通道列表的
    /// </summary>
    public IReadOnlyList<ExrChannel> channels { get; init; } = [];

    /// <summary>
    ///     压缩类型的
    /// </summary>
    public ExrCompression compression { get; init; }

    /// <summary>
    ///     像素类型的
    /// </summary>
    public ExrPixelType pixel_type { get; init; }

    /// <summary>
    ///     显示窗口的
    /// </summary>
    public ExrBox2I display_window { get; init; }

    /// <summary>
    ///     数据窗口的
    /// </summary>
    public ExrBox2I data_window { get; init; }
}

/// <summary>
///     EXR 通道的
/// </summary>
public sealed class ExrChannel
{
    /// <summary>
    ///     通道名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     像素类型的
    /// </summary>
    public ExrPixelType pixel_type { get; init; }
}

/// <summary>
///     EXR 2D 框（整数）的
/// </summary>
public sealed class ExrBox2I
{
    /// <summary>
    ///     X 最小值的
    /// </summary>
    public int x_min { get; init; }

    /// <summary>
    ///     Y 最小值的
    /// </summary>
    public int y_min { get; init; }

    /// <summary>
    ///     X 最大值的
    /// </summary>
    public int x_max { get; init; }

    /// <summary>
    ///     Y 最大值的
    /// </summary>
    public int y_max { get; init; }
}