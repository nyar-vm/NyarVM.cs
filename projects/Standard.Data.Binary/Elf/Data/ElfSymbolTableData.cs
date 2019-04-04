namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 符号表数据的
/// </summary>
public sealed class ElfSymbolTableData
{
    /// <summary>
    ///     符号条目列表的
    /// </summary>
    public IReadOnlyList<ElfSymbolData> symbols { get; init; } = [];
}