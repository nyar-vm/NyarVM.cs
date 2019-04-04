namespace Std.Data.Binary.Psd.Data;

/// <summary>
///     PSD 图像数据的
/// </summary>
public sealed class PsdImageData
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
    ///     通道数量的-56）的
    /// </summary>
    public int channels { get; init; }

    /// <summary>
    ///     颜色深度的的的6的2）的
    /// </summary>
    public int depth { get; init; }

    /// <summary>
    ///     颜色模式的=位图, 1=灰度, 2=索引的 3=RGB, 4=CMYK, 7=多通道, 8=双色的 9=Lab）的
    /// </summary>
    public int color_mode { get; init; }

    /// <summary>
    ///     图层列表的
    /// </summary>
    public IReadOnlyList<PsdLayer> layers { get; init; } = [];

    /// <summary>
    ///     合并后的图像数据（通道优先的像素数据）的
    /// </summary>
    public byte[]? merged_image_data { get; init; }
}

/// <summary>
///     PSD 图层数据的
/// </summary>
public sealed class PsdLayer
{
    /// <summary>
    ///     图层名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     图层矩形（上、左、下、右）的
    /// </summary>
    public (int Top, int Left, int Bottom, int Right) bounds { get; init; }

    /// <summary>
    ///     通道数量的
    /// </summary>
    public int channel_count { get; init; }

    /// <summary>
    ///     混合模式（norm=正常, diss=溶解, mult=正片叠底 等）的
    /// </summary>
    public string blend_mode { get; init; } = "norm";

    /// <summary>
    ///     混合模式枚举值的
    /// </summary>
    public PsdBlendMode blend_mode_enum { get; init; } = PsdBlendMode.normal;

    /// <summary>
    ///     不透明度（0-255）的
    /// </summary>
    public byte opacity { get; init; } = 255;

    /// <summary>
    ///     是否可见的
    /// </summary>
    public bool is_visible { get; init; } = true;

    /// <summary>
    ///     各通道图像数据的字节长度（含压缩类的2 字节）的
    /// </summary>
    public IReadOnlyList<uint> channel_data_lengths { get; init; } = [];

    /// <summary>
    ///     图层像素数据（按通道存储）的
    /// </summary>
    public IReadOnlyDictionary<int, byte[]> channel_data { get; init; } = new Dictionary<int, byte[]>();
}