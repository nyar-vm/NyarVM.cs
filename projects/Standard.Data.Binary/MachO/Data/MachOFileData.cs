namespace Std.Data.Binary.MachO.Data;

/// <summary>
///     Mach-O 文件头数据的
/// </summary>
public sealed class MachOHeaderData
{
    /// <summary>
    ///     魔数的xFEEDFACE=32的 0xFEEDFACF=64位）的
    /// </summary>
    public uint magic { get; init; }

    /// <summary>
    ///     CPU 类型的
    /// </summary>
    public int cpu_type { get; init; }

    /// <summary>
    ///     CPU 子类型的
    /// </summary>
    public int cpu_subtype { get; init; }

    /// <summary>
    ///     文件类型的
    /// </summary>
    public uint file_type { get; init; }

    /// <summary>
    ///     加载命令数量的
    /// </summary>
    public uint number_of_load_commands { get; init; }

    /// <summary>
    ///     加载命令总大小的
    /// </summary>
    public uint size_of_load_commands { get; init; }

    /// <summary>
    ///     标志的
    /// </summary>
    public uint flags { get; init; }

    /// <summary>
    ///     保留的4位）的
    /// </summary>
    public uint reserved { get; init; }

    /// <summary>
    ///     是否为小端序的
    /// </summary>
    public bool is_little_endian { get; init; } = true;

    /// <summary>
    ///     是否的64 位的
    /// </summary>
    public bool is64_bit => magic == 0xFEEDFACF;

    /// <summary>
    ///     是否为可执行文件的
    /// </summary>
    public bool is_executable => file_type == 2;

    /// <summary>
    ///     是否为动态库的
    /// </summary>
    public bool is_dynamic_library => file_type == 6;
}

/// <summary>
///     Mach-O 加载命令数据的
/// </summary>
public sealed class MachOLoadCommandData
{
    /// <summary>
    ///     命令类型的
    /// </summary>
    public uint command { get; init; }

    /// <summary>
    ///     命令大小的
    /// </summary>
    public uint size { get; init; }

    /// <summary>
    ///     命令数据的
    /// </summary>
    public byte[] data { get; init; } = [];
}

/// <summary>
///     Mach-O 节区数据的
/// </summary>
public sealed class MachOSectionData
{
    /// <summary>
    ///     节区名称的
    /// </summary>
    public string section_name { get; init; } = string.Empty;

    /// <summary>
    ///     段名称的
    /// </summary>
    public string segment_name { get; init; } = string.Empty;

    /// <summary>
    ///     内存地址的
    /// </summary>
    public ulong address { get; init; }

    /// <summary>
    ///     大小的
    /// </summary>
    public ulong size { get; init; }

    /// <summary>
    ///     文件偏移的
    /// </summary>
    public uint offset { get; init; }

    /// <summary>
    ///     对齐的
    /// </summary>
    public uint alignment { get; init; }

    /// <summary>
    ///     重定位文件偏移的
    /// </summary>
    public uint relocations_offset { get; init; }

    /// <summary>
    ///     重定位数量的
    /// </summary>
    public uint number_of_relocations { get; init; }

    /// <summary>
    ///     标志的
    /// </summary>
    public uint flags { get; init; }

    /// <summary>
    ///     节区原始内容数据的
    /// </summary>
    public byte[] content { get; set; } = [];
}

/// <summary>
///     Mach-O 文件数据的
/// </summary>
public sealed class MachOFileData
{
    /// <summary>
    ///     Mach-O 头数据的
    /// </summary>
    public MachOHeaderData header { get; init; } = new();

    /// <summary>
    ///     加载命令列表的
    /// </summary>
    public IReadOnlyList<MachOLoadCommandData> load_commands { get; init; } = [];

    /// <summary>
    ///     节区列表的
    /// </summary>
    public IReadOnlyList<MachOSectionData> sections { get; init; } = [];
}