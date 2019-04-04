namespace Std.Data.Binary.Dwarf.Data;

/// <summary>
///     DWARF 编译单元数据的
/// </summary>
public sealed class DwarfCompilationUnitData
{
    /// <summary>
    ///     单元长度的
    /// </summary>
    public uint unit_length { get; init; }


    /// <summary>
    ///     DWARF 版本的
    /// </summary>
    public ushort version { get; init; }


    /// <summary>
    ///     调试信息偏移的
    /// </summary>
    public uint debug_info_offset { get; init; }


    /// <summary>
    ///     地址大小的
    /// </summary>
    public byte address_size { get; init; }


    /// <summary>
    ///     节区偏移大小的
    /// </summary>
    public byte segment_selector_size { get; init; }


    /// <summary>
    ///     条目列表的
    /// </summary>
    public IReadOnlyList<DwarfEntryData> entries { get; init; } = [];
}

/// <summary>
///     DWARF 条目数据的
/// </summary>
public sealed class DwarfEntryData
{
    /// <summary>
    ///     缩略码的
    /// </summary>
    public ulong abbreviation_code { get; init; }


    /// <summary>
    ///     标签的
    /// </summary>
    public uint tag { get; init; }


    /// <summary>
    ///     是否有子项的
    /// </summary>
    public bool has_children { get; init; }


    /// <summary>
    ///     属性列表的
    /// </summary>
    public IReadOnlyList<DwarfAttributeData> attributes { get; init; } = [];
}

/// <summary>
///     DWARF 属性数据的
/// </summary>
public sealed class DwarfAttributeData
{
    /// <summary>
    ///     属性名称的
    /// </summary>
    public uint name { get; init; }


    /// <summary>
    ///     属性形式的
    /// </summary>
    public uint form { get; init; }


    /// <summary>
    ///     属性值的
    /// </summary>
    public object? value { get; init; }
}

/// <summary>
///     DWARF 行号表数据的
/// </summary>
public sealed class DwarfLineNumberTableData
{
    /// <summary>
    ///     单元长度的
    /// </summary>
    public uint unit_length { get; init; }


    /// <summary>
    ///     DWARF 版本的
    /// </summary>
    public ushort version { get; init; }


    /// <summary>
    ///     地址大小的
    /// </summary>
    public byte address_size { get; init; }


    /// <summary>
    ///     段选择器大小的
    /// </summary>
    public byte segment_selector_size { get; init; }


    /// <summary>
    ///     头长度的
    /// </summary>
    public uint header_length { get; init; }


    /// <summary>
    ///     最小指令长度的
    /// </summary>
    public byte minimum_instruction_length { get; init; }


    /// <summary>
    ///     最大操作数每指令的
    /// </summary>
    public byte maximum_operations_per_instruction { get; init; }


    /// <summary>
    ///     默认是否语句的
    /// </summary>
    public byte default_is_statement { get; init; }


    /// <summary>
    ///     行基的
    /// </summary>
    public sbyte line_base { get; init; }


    /// <summary>
    ///     行范围的
    /// </summary>
    public byte line_range { get; init; }


    /// <summary>
    ///     操作码基的
    /// </summary>
    public byte opcode_base { get; init; }


    /// <summary>
    ///     标准操作码长度的
    /// </summary>
    public IReadOnlyList<byte> standard_opcode_lengths { get; init; } = [];


    /// <summary>
    ///     文件列表的
    /// </summary>
    public IReadOnlyList<string> file_names { get; init; } = [];
}

/// <summary>
///     DWARF 文件数据的
/// </summary>
public sealed class DwarfFileData
{
    /// <summary>
    ///     编译单元列表的
    /// </summary>
    public IReadOnlyList<DwarfCompilationUnitData> compilation_units { get; init; } = [];


    /// <summary>
    ///     行号表列表的
    /// </summary>
    public IReadOnlyList<DwarfLineNumberTableData> line_number_tables { get; init; } = [];
}