namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 文件数据的
/// </summary>
public sealed class ElfFileData
{
    /// <summary>
    ///     ELF 头数据的
    /// </summary>
    public ElfHeaderData header { get; init; } = new();

    /// <summary>
    ///     节区头列表的
    /// </summary>
    public IReadOnlyList<ElfSectionHeaderData> section_headers { get; init; } = [];

    /// <summary>
    ///     程序头列表的
    /// </summary>
    public IReadOnlyList<ElfProgramHeaderData> program_headers { get; init; } = [];

    /// <summary>
    ///     符号表数据（可为 null）的
    /// </summary>
    public ElfSymbolTableData? symbol_table { get; init; }

    /// <summary>
    ///     是否为可执行文件的
    /// </summary>
    public bool is_executable => header.type == ElfConstants.type_executable;

    /// <summary>
    ///     是否为共享库的
    /// </summary>
    public bool is_shared_library => header.type == ElfConstants.type_shared_library;
}