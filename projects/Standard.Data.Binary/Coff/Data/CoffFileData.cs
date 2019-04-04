namespace Std.Data.Binary.Coff.Data;

/// <summary>
///     COFF 文件数据�?
/// </summary>
public sealed class CoffFileData
{
    /// <summary>
    ///     COFF 头数据的
    /// </summary>
    public CoffHeaderData header { get; init; } = new();

    /// <summary>
    ///     节区头列表的
    /// </summary>
    public IReadOnlyList<CoffSectionHeaderData> sections { get; init; } = [];

    /// <summary>
    ///     符号表的
    /// </summary>
    public IReadOnlyList<CoffSymbolData> symbols { get; init; } = [];

    /// <summary>
    ///     重定位列表的
    /// </summary>
    public IReadOnlyList<CoffRelocationData> relocations { get; init; } = [];
}