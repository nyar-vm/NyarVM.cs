namespace Std.Data.Binary.Ttf.Data;

/// <summary>
///     TrueType/OpenType 字体文件数据的
/// </summary>
public sealed class TtfFontData
{
    /// <summary>
    ///     字体类型的
    /// </summary>
    public TtfFontType font_type { get; init; }

    /// <summary>
    ///     表数量的
    /// </summary>
    public ushort table_count { get; init; }

    /// <summary>
    ///     字体表记录的
    /// </summary>
    public IReadOnlyList<TtfTableRecord> tables { get; init; } = [];

    /// <summary>
    ///     字体头部信息（来的head 表）的
    /// </summary>
    public TtfHeadInfo? head_info { get; init; }

    /// <summary>
    ///     字体名称信息（来的name 表）的
    /// </summary>
    public TtfNameInfo? name_info { get; init; }
}

/// <summary>
///     字体类型的
/// </summary>
public enum TtfFontType
{
    /// <summary>
    ///     TrueType 轮廓的
    /// </summary>
    true_type,

    /// <summary>
    ///     CFF 轮廓（OpenType）的
    /// </summary>
    cff,

    /// <summary>
    ///     TrueType 集合的
    /// </summary>
    collection,

    /// <summary>
    ///     未知类型的
    /// </summary>
    unknown
}

/// <summary>
///     字体表记录的
/// </summary>
public sealed class TtfTableRecord
{
    /// <summary>
    ///     表标签（4 字节 ASCII）的
    /// </summary>
    public string tag { get; init; } = string.Empty;

    /// <summary>
    ///     表校验和的
    /// </summary>
    public uint checksum { get; init; }

    /// <summary>
    ///     表偏移量的
    /// </summary>
    public uint offset { get; init; }

    /// <summary>
    ///     表长度的
    /// </summary>
    public uint length { get; init; }

    /// <summary>
    ///     表原始数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}

/// <summary>
///     字体头部信息（head 表）的
/// </summary>
public sealed class TtfHeadInfo
{
    /// <summary>
    ///     字体版本的
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     单位的Em的
    /// </summary>
    public ushort units_per_em { get; init; }

    /// <summary>
    ///     创建时间的
    /// </summary>
    public long created { get; init; }

    /// <summary>
    ///     修改时间的
    /// </summary>
    public long modified { get; init; }

    /// <summary>
    ///     X 最小值的
    /// </summary>
    public short x_min { get; init; }

    /// <summary>
    ///     Y 最小值的
    /// </summary>
    public short y_min { get; init; }

    /// <summary>
    ///     X 最大值的
    /// </summary>
    public short x_max { get; init; }

    /// <summary>
    ///     Y 最大值的
    /// </summary>
    public short y_max { get; init; }
}

/// <summary>
///     字体名称信息（name 表）的
/// </summary>
public sealed class TtfNameInfo
{
    /// <summary>
    ///     字体族名称的
    /// </summary>
    public string family_name { get; init; } = string.Empty;

    /// <summary>
    ///     字体子族名称的
    /// </summary>
    public string sub_family_name { get; init; } = string.Empty;

    /// <summary>
    ///     完整字体名称的
    /// </summary>
    public string full_name { get; init; } = string.Empty;

    /// <summary>
    ///     版本字符串的
    /// </summary>
    public string version { get; init; } = string.Empty;
}