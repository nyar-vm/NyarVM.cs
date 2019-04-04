namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 文件头数据的
/// </summary>
public sealed class PeHeaderData
{
    /// <summary>
    ///     DOS 头魔数（"MZ"）的
    /// </summary>
    public ushort dos_magic { get; init; }

    /// <summary>
    ///     PE 头偏移的
    /// </summary>
    public uint pe_header_offset { get; init; }

    /// <summary>
    ///     PE 头魔数（"PE\0\0"）的
    /// </summary>
    public uint pe_magic { get; init; }

    /// <summary>
    ///     机器类型的
    /// </summary>
    public ushort machine { get; init; }

    /// <summary>
    ///     节区数量的
    /// </summary>
    public ushort number_of_sections { get; init; }

    /// <summary>
    ///     时间戳的
    /// </summary>
    public uint time_date_stamp { get; init; }

    /// <summary>
    ///     符号表偏移的
    /// </summary>
    public uint pointer_to_symbol_table { get; init; }

    /// <summary>
    ///     符号数量的
    /// </summary>
    public uint number_of_symbols { get; init; }

    /// <summary>
    ///     可选头大小的
    /// </summary>
    public ushort size_of_optional_header { get; init; }

    /// <summary>
    ///     特征标志的
    /// </summary>
    public ushort characteristics { get; init; }
}