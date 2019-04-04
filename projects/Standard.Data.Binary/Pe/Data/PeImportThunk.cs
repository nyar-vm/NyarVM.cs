namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     导入 thunk 条目，对的IMAGE_THUNK_DATA的
/// </summary>
public sealed class PeImportThunk
{
    /// <summary>
    ///     原始 thunk 值（ordinal flag + RVA 的ordinal）的
    /// </summary>
    public ulong value { get; init; }

    /// <summary>
    ///     是否为按序数导入的
    /// </summary>
    public bool is_ordinal { get; init; }

    /// <summary>
    ///     序数值（仅当 IsOrdinal 的true 时有效）的
    /// </summary>
    public ushort ordinal { get; init; }

    /// <summary>
    ///     函数名称（按名称导入时填充）的
    /// </summary>
    public string name { get; set; } = string.Empty;
}