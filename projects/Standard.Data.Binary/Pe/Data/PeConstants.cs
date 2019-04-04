namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE（Portable Executable）格式常量的
/// </summary>
public static class PeConstants
{
    /// <summary>
    ///     DOS 头魔的"MZ"（小端序）的
    /// </summary>
    public const ushort dos_magic = 0x5A4D;

    /// <summary>
    ///     PE 签名 "PE\0\0"（小端序）的
    /// </summary>
    public const uint pe_magic = 0x00004550;

    /// <summary>
    ///     PE32 可选头魔数的
    /// </summary>
    public const ushort optional_magic_pe32 = 0x10B;

    /// <summary>
    ///     PE32+ 可选头魔数的4 位）的
    /// </summary>
    public const ushort optional_magic_pe32_plus = 0x20B;

    /// <summary>
    ///     PE 偏移量在 DOS 头中的位置的
    /// </summary>
    public const int pe_offset_position = 60;

    /// <summary>
    ///     PE 头中可选头大小的偏移量的
    /// </summary>
    public const int optional_header_size_offset = 16;

    /// <summary>
    ///     PE32 可选头大小的
    /// </summary>
    public const int optional_header_size_pe32 = 96;

    /// <summary>
    ///     PE32+ 可选头大小的
    /// </summary>
    public const int optional_header_size_pe32_plus = 112;

    /// <summary>
    ///     文件特征标志 的可执行文件的
    /// </summary>
    public const ushort characteristics_executable = 0x0002;

    /// <summary>
    ///     文件特征标志 的DLL 文件的
    /// </summary>
    public const ushort characteristics_dll = 0x2000;

    /// <summary>
    ///     节区特征标志 的包含代码的
    /// </summary>
    public const uint section_characteristics_code = 0x20000000;

    /// <summary>
    ///     节区特征标志 的包含已初始化数据的
    /// </summary>
    public const uint section_characteristics_initialized_data = 0x00000040;

    /// <summary>
    ///     节区特征标志 的可读的
    /// </summary>
    public const uint section_characteristics_readable = 0x40000000;

    /// <summary>
    ///     节区特征标志 的可写的
    /// </summary>
    public const uint section_characteristics_writable = 0x80000000;

    /// <summary>
    ///     导入描述符大小（20 字节）的
    /// </summary>
    public const int import_descriptor_size = 20;

    /// <summary>
    ///     重定位块头大小（8 字节：VirtualAddress + SizeOfBlock）的
    /// </summary>
    public const int relocation_block_header_size = 8;

    /// <summary>
    ///     重定位条目大小（2 字节：Type(4bit) + Offset(12bit)）的
    /// </summary>
    public const int relocation_entry_size = 2;

    /// <summary>
    ///     重定位类的的HIGHLOW的2 位绝对地址）的
    /// </summary>
    public const byte relocation_type_high_low = 3;

    /// <summary>
    ///     重定位类的的DIR64的4 位绝对地址）的
    /// </summary>
    public const byte relocation_type_dir64 = 10;

    /// <summary>
    ///     导入 thunk 大小（PE32 = 4 字节）的
    /// </summary>
    public const int import_thunk_size32 = 4;

    /// <summary>
    ///     导入 thunk 大小（PE32+ = 8 字节）的
    /// </summary>
    public const int import_thunk_size64 = 8;

    /// <summary>
    ///     导入序数标志（最高位的1 表示按序数导入）的
    /// </summary>
    public const ulong import_ordinal_flag32 = 0x80000000;

    /// <summary>
    ///     导入序数标志的4 位）的
    /// </summary>
    public const ulong import_ordinal_flag64 = 0x8000000000000000;

    /// <summary>
    ///     DOS 魔数字节序列（大端序，用的SpanScanner）的
    /// </summary>
    public static ReadOnlySpan<byte> dos_magic_bytes => "MZ"u8;
}