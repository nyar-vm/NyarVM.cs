namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     导入描述符，对应 IMAGE_IMPORT_DESCRIPTOR的
/// </summary>
public sealed class PeImportDescriptor
{
    /// <summary>
    ///     原始 IAT（Import Name Table）的 RVA的
    /// </summary>
    public uint original_first_thunk { get; init; }

    /// <summary>
    ///     时间戳的
    /// </summary>
    public uint time_date_stamp { get; init; }

    /// <summary>
    ///     转发链索引的
    /// </summary>
    public uint forwarder_chain { get; init; }

    /// <summary>
    ///     DLL 名称字符串的 RVA的
    /// </summary>
    public uint name_rva { get; init; }

    /// <summary>
    ///     IAT（Import Address Table）的 RVA的
    /// </summary>
    public uint first_thunk { get; init; }

    /// <summary>
    ///     DLL 名称（解码后填充）的
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     导入函数列表的
    /// </summary>
    public IReadOnlyList<PeImportThunk> thunks { get; init; } = [];
}