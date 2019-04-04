namespace Std.Data.Binary.Ttf.Data;

/// <summary>
///     TrueType/OpenType 字体格式常量的
/// </summary>
public static class TtfConstants
{
    /// <summary>
    ///     TrueType 字体魔数的x00010000）的
    /// </summary>
    public const uint true_type_magic = 0x00010000;

    /// <summary>
    ///     偏移表大小的
    /// </summary>
    public const int offset_table_size = 12;

    /// <summary>
    ///     表记录大小的
    /// </summary>
    public const int table_record_size = 16;

    /// <summary>
    ///     OpenType CFF 字体魔数的OTTO"）的
    /// </summary>
    public static ReadOnlySpan<byte> cff_magic => "OTTO"u8.ToArray();

    /// <summary>
    ///     TrueType 集合字体魔数的ttcf"）的
    /// </summary>
    public static ReadOnlySpan<byte> collection_magic => "ttcf"u8.ToArray();
}

/// <summary>
///     字体表名称常量的
/// </summary>
public static class TtfTableNames
{
    /// <summary>
    ///     字体头部表的
    /// </summary>
    public const string head = "head";

    /// <summary>
    ///     水平头部表的
    /// </summary>
    public const string h_head = "hhea";

    /// <summary>
    ///     水平度量表的
    /// </summary>
    public const string h_mtx = "hmtx";

    /// <summary>
    ///     最大轮廓表的
    /// </summary>
    public const string max_p = "maxp";

    /// <summary>
    ///     字符到字形映射表的
    /// </summary>
    public const string c_map = "cmap";

    /// <summary>
    ///     命名表的
    /// </summary>
    public const string name = "name";

    /// <summary>
    ///     OS/2 的Windows 度量表的
    /// </summary>
    public const string os2 = "OS/2";

    /// <summary>
    ///     位置表的
    /// </summary>
    public const string post = "post";

    /// <summary>
    ///     字形数据表的
    /// </summary>
    public const string glyf = "glyf";

    /// <summary>
    ///     位置索引表的
    /// </summary>
    public const string loca = "loca";

    /// <summary>
    ///     CFF 轮廓数据的
    /// </summary>
    public const string cff = "CFF ";
}