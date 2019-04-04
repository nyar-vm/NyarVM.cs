namespace Std.Data.Binary.BmFont.Data;

/// <summary>
///     BMFont 位图字体文件数据的
/// </summary>
public sealed class BmFontData
{
    /// <summary>
    ///     字体信息的
    /// </summary>
    public BmFontInfo info { get; init; } = new();

    /// <summary>
    ///     通用信息的
    /// </summary>
    public BmFontCommon common { get; init; } = new();

    /// <summary>
    ///     页面名称列表的
    /// </summary>
    public IReadOnlyList<string> pages { get; init; } = [];

    /// <summary>
    ///     字符列表的
    /// </summary>
    public IReadOnlyList<BmFontChar> chars { get; init; } = [];

    /// <summary>
    ///     字距对列表的
    /// </summary>
    public IReadOnlyList<BmFontKerningPair> kerning_pairs { get; init; } = [];
}

/// <summary>
///     BMFont 字体信息的
/// </summary>
public sealed class BmFontInfo
{
    /// <summary>
    ///     字体大小的
    /// </summary>
    public short size { get; init; }

    /// <summary>
    ///     位深度（8/32）的
    /// </summary>
    public byte bit_depth { get; init; }

    /// <summary>
    ///     是否使用粗体的
    /// </summary>
    public bool bold { get; init; }

    /// <summary>
    ///     是否使用斜体的
    /// </summary>
    public bool italic { get; init; }

    /// <summary>
    ///     字体字符集的
    /// </summary>
    public byte char_set { get; init; }

    /// <summary>
    ///     是否使用 Unicode的
    /// </summary>
    public bool unicode { get; init; }

    /// <summary>
    ///     水平间距的
    /// </summary>
    public short spacing_h { get; init; }

    /// <summary>
    ///     垂直间距的
    /// </summary>
    public short spacing_v { get; init; }

    /// <summary>
    ///     行高的
    /// </summary>
    public short line_height { get; init; }

    /// <summary>
    ///     字体名称的
    /// </summary>
    public string font_name { get; init; } = string.Empty;
}

/// <summary>
///     BMFont 通用信息的
/// </summary>
public sealed class BmFontCommon
{
    /// <summary>
    ///     行高的
    /// </summary>
    public ushort line_height { get; init; }

    /// <summary>
    ///     基线高度的
    /// </summary>
    public ushort @base { get; init; }

    /// <summary>
    ///     纹理宽度的
    /// </summary>
    public ushort scale_w { get; init; }

    /// <summary>
    ///     纹理高度的
    /// </summary>
    public ushort scale_h { get; init; }

    /// <summary>
    ///     页面数量的
    /// </summary>
    public ushort pages { get; init; }

    /// <summary>
    ///     是否使用 Alpha 通道的
    /// </summary>
    public bool alpha_channel { get; init; }

    /// <summary>
    ///     是否使用红色通道的
    /// </summary>
    public bool red_channel { get; init; }

    /// <summary>
    ///     是否使用绿色通道的
    /// </summary>
    public bool green_channel { get; init; }

    /// <summary>
    ///     是否使用蓝色通道的
    /// </summary>
    public bool blue_channel { get; init; }

    /// <summary>
    ///     是否打包的
    /// </summary>
    public bool packed { get; init; }
}

/// <summary>
///     BMFont 字符信息的
/// </summary>
public sealed class BmFontChar
{
    /// <summary>
    ///     字符 ID（Unicode 码点）的
    /// </summary>
    public uint id { get; init; }

    /// <summary>
    ///     X 坐标的
    /// </summary>
    public ushort x { get; init; }

    /// <summary>
    ///     Y 坐标的
    /// </summary>
    public ushort y { get; init; }

    /// <summary>
    ///     宽度的
    /// </summary>
    public ushort width { get; init; }

    /// <summary>
    ///     高度的
    /// </summary>
    public ushort height { get; init; }

    /// <summary>
    ///     X 偏移的
    /// </summary>
    public short x_offset { get; init; }

    /// <summary>
    ///     Y 偏移的
    /// </summary>
    public short y_offset { get; init; }

    /// <summary>
    ///     X 前进量的
    /// </summary>
    public short x_advance { get; init; }

    /// <summary>
    ///     页面索引的
    /// </summary>
    public byte page { get; init; }

    /// <summary>
    ///     通道的
    /// </summary>
    public BmFontChannel channel { get; init; }
}

/// <summary>
///     BMFont 字距对的
/// </summary>
public sealed class BmFontKerningPair
{
    /// <summary>
    ///     第一个字的ID的
    /// </summary>
    public uint first { get; init; }

    /// <summary>
    ///     第二个字的ID的
    /// </summary>
    public uint second { get; init; }

    /// <summary>
    ///     字距调整量的
    /// </summary>
    public short amount { get; init; }
}