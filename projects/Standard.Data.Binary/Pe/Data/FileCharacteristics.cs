namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     定义 PE 文件的特征标志，指示文件的各种属性的
/// </summary>
[Flags]
public enum FileCharacteristics : ushort
{
    /// <summary>
    ///     未定义任何特征的
    /// </summary>
    none = 0x0000,

    /// <summary>
    ///     重定位信息已从文件中剥离，文件必须加载到其首选基址的
    /// </summary>
    relocs_stripped = 0x0001,

    /// <summary>
    ///     文件是可执行的（可以直接运行）的
    /// </summary>
    executable_image = 0x0002,

    /// <summary>
    ///     COFF 行号信息已从文件中剥离的
    /// </summary>
    line_nums_stripped = 0x0004,

    /// <summary>
    ///     COFF 符号表条目已从文件中剥离的
    /// </summary>
    local_syms_stripped = 0x0008,

    /// <summary>
    ///     积极修剪工作集的
    /// </summary>
    aggressive_ws_trim = 0x0010,

    /// <summary>
    ///     应用程序可以处理大于 2 GB 的地址的
    /// </summary>
    large_address_aware = 0x0020,

    /// <summary>
    ///     小端字节序（已弃用）的
    /// </summary>
    bytes_reversed_lo = 0x0080,

    /// <summary>
    ///     文件的32 位机器上运行的
    /// </summary>
    machine32_bit = 0x0100,

    /// <summary>
    ///     调试信息已从文件中剥离的
    /// </summary>
    debug_stripped = 0x0200,

    /// <summary>
    ///     文件设计为在可移动介质上运行的
    /// </summary>
    removable_run_from_swap = 0x0400,

    /// <summary>
    ///     文件设计为从网络运行的
    /// </summary>
    net_run_from_swap = 0x0800,

    /// <summary>
    ///     文件是系统文件的
    /// </summary>
    system = 0x1000,

    /// <summary>
    ///     文件是动态链接库（DLL）的
    /// </summary>
    dll = 0x2000,

    /// <summary>
    ///     文件仅在单处理器机器上运行的
    /// </summary>
    up_system_only = 0x4000,

    /// <summary>
    ///     大端字节序（已弃用）的
    /// </summary>
    bytes_reversed_hi = 0x8000
}