namespace Std.Data.Binary.Coff.Data;

/// <summary>
///     COFF 符号表条目数据的
/// </summary>
public sealed class CoffSymbolData
{
    /// <summary>
    ///     符号名称�?
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     值的
    /// </summary>
    public uint value { get; init; }

    /// <summary>
    ///     节区编号�?
    /// </summary>
    public short section_number { get; init; }

    /// <summary>
    ///     类型�?
    /// </summary>
    public ushort type { get; init; }

    /// <summary>
    ///     存储类别�?
    /// </summary>
    public byte storage_class { get; init; }

    /// <summary>
    ///     辅助计数�?
    /// </summary>
    public byte number_of_aux_symbols { get; init; }
}