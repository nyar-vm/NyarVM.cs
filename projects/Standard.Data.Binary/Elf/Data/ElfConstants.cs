namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF（Executable and Linkable Format）格式常量的
/// </summary>
public static class ElfConstants
{
    /// <summary>
    ///     ELF 文件类别 的32 位（ELFCLASS32）的
    /// </summary>
    public const byte class32 = 1;

    /// <summary>
    ///     ELF 文件类别 的64 位（ELFCLASS64）的
    /// </summary>
    public const byte class64 = 2;

    /// <summary>
    ///     数据编码 的小端序（ELFDATA2LSB）的
    /// </summary>
    public const byte data_encoding_little_endian = 1;

    /// <summary>
    ///     数据编码 的大端序（ELFDATA2MSB）的
    /// </summary>
    public const byte data_encoding_big_endian = 2;

    /// <summary>
    ///     文件类型 的可执行文件（ET_EXEC）的
    /// </summary>
    public const ushort type_executable = 2;

    /// <summary>
    ///     文件类型 的共享库（ET_DYN）的
    /// </summary>
    public const ushort type_shared_library = 3;

    /// <summary>
    ///     符号绑定 的局部符号（STB_LOCAL）的
    /// </summary>
    public const byte symbol_bind_local = 0;

    /// <summary>
    ///     符号绑定 的全局符号（STB_GLOBAL）的
    /// </summary>
    public const byte symbol_bind_global = 1;

    /// <summary>
    ///     符号绑定 的弱符号（STB_WEAK）的
    /// </summary>
    public const byte symbol_bind_weak = 2;

    /// <summary>
    ///     符号类型 的未指定类型（STT_NOTYPE）的
    /// </summary>
    public const byte symbol_type_no_type = 0;

    /// <summary>
    ///     符号类型 的对象/数据（STT_OBJECT）的
    /// </summary>
    public const byte symbol_type_object = 1;

    /// <summary>
    ///     符号类型 的函数/代码（STT_FUNC）的
    /// </summary>
    public const byte symbol_type_func = 2;

    /// <summary>
    ///     符号类型 的节区（STT_SECTION）的
    /// </summary>
    public const byte symbol_type_section = 3;

    /// <summary>
    ///     符号类型 的文件名（STT_FILE）的
    /// </summary>
    public const byte symbol_type_file = 4;

    /// <summary>
    ///     特殊节区索引 的未定义（SHN_UNDEF）的
    /// </summary>
    public const ushort section_index_undefined = 0;

    /// <summary>
    ///     特殊节区索引 的绝对地址（SHN_ABS）的
    /// </summary>
    public const ushort section_index_absolute = 0xFFF1;

    /// <summary>
    ///     特殊节区索引 的COMMON 块（SHN_COMMON）的
    /// </summary>
    public const ushort section_index_common = 0xFFF2;

    /// <summary>
    ///     64 位符号表条目大小（Elf64_Sym = 24 字节）的
    /// </summary>
    public const int symbol_entry_size64 = 24;

    /// <summary>
    ///     32 位符号表条目大小（Elf32_Sym = 16 字节）的
    /// </summary>
    public const int symbol_entry_size32 = 16;

    /// <summary>
    ///     ELF 魔数字节序列的x7F 'E' 'L' 'F'）的
    /// </summary>
    public static ReadOnlySpan<byte> magic => [0x7F, 0x45, 0x4C, 0x46];
}