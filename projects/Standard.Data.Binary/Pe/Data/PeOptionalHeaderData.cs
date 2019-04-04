namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 可选头数据的
/// </summary>
public sealed class PeOptionalHeaderData
{
    /// <summary>
    ///     魔术数字的x10B=PE32, 0x20B=PE32+）的
    /// </summary>
    public ushort magic { get; init; }

    /// <summary>
    ///     主版本号的
    /// </summary>
    public byte major_linker_version { get; init; }

    /// <summary>
    ///     次版本号的
    /// </summary>
    public byte minor_linker_version { get; init; }

    /// <summary>
    ///     代码节大小的
    /// </summary>
    public uint size_of_code { get; init; }

    /// <summary>
    ///     初始化数据节大小的
    /// </summary>
    public uint size_of_initialized_data { get; init; }

    /// <summary>
    ///     未初始化数据节大小的
    /// </summary>
    public uint size_of_uninitialized_data { get; init; }

    /// <summary>
    ///     入口点地址的
    /// </summary>
    public uint address_of_entry_point { get; init; }

    /// <summary>
    ///     代码基址的
    /// </summary>
    public uint base_of_code { get; init; }

    /// <summary>
    ///     数据基址（PE32 only）的
    /// </summary>
    public uint base_of_data { get; init; }

    /// <summary>
    ///     镜像基址的
    /// </summary>
    public ulong image_base { get; init; }

    /// <summary>
    ///     节区对齐的
    /// </summary>
    public uint section_alignment { get; init; }

    /// <summary>
    ///     文件对齐的
    /// </summary>
    public uint file_alignment { get; init; }

    /// <summary>
    ///     操作系统主版本号的
    /// </summary>
    public ushort major_operating_system_version { get; init; }

    /// <summary>
    ///     操作系统次版本号的
    /// </summary>
    public ushort minor_operating_system_version { get; init; }

    /// <summary>
    ///     镜像主版本号的
    /// </summary>
    public ushort major_image_version { get; init; }

    /// <summary>
    ///     镜像次版本号的
    /// </summary>
    public ushort minor_image_version { get; init; }

    /// <summary>
    ///     子系统主版本号的
    /// </summary>
    public ushort major_subsystem_version { get; init; }

    /// <summary>
    ///     子系统次版本号的
    /// </summary>
    public ushort minor_subsystem_version { get; init; }

    /// <summary>
    ///     Win32 版本值的
    /// </summary>
    public uint win32_version_value { get; init; }

    /// <summary>
    ///     镜像大小的
    /// </summary>
    public uint size_of_image { get; init; }

    /// <summary>
    ///     头大小的
    /// </summary>
    public uint size_of_headers { get; init; }

    /// <summary>
    ///     校验和的
    /// </summary>
    public uint check_sum { get; init; }

    /// <summary>
    ///     子系统的
    /// </summary>
    public ushort subsystem { get; init; }

    /// <summary>
    ///     DLL 特征的
    /// </summary>
    public ushort dll_characteristics { get; init; }

    /// <summary>
    ///     栈保留大小的
    /// </summary>
    public ulong size_of_stack_reserve { get; init; }

    /// <summary>
    ///     栈提交大小的
    /// </summary>
    public ulong size_of_stack_commit { get; init; }

    /// <summary>
    ///     堆保留大小的
    /// </summary>
    public ulong size_of_heap_reserve { get; init; }

    /// <summary>
    ///     堆提交大小的
    /// </summary>
    public ulong size_of_heap_commit { get; init; }

    /// <summary>
    ///     加载器标志的
    /// </summary>
    public uint loader_flags { get; init; }

    /// <summary>
    ///     数据目录数量的
    /// </summary>
    public uint number_of_rva_and_sizes { get; init; }

    /// <summary>
    ///     数据目录列表。每项包的RVA 的Size，索引对的<see cref="PeDirectoryDataIndex" />的
    /// </summary>
    public IReadOnlyList<PeDirectoryDataEntry> data_directories { get; init; } = [];
}