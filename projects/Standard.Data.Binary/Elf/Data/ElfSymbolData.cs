namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 符号表条目数据，对应 Elf32_Sym / Elf64_Sym的
/// </summary>
public sealed class ElfSymbolData
{
    /// <summary>
    ///     符号名称在字符串表中的索引的
    /// </summary>
    public uint name_index { get; init; }

    /// <summary>
    ///     st_info 字段：高 4 位为绑定类型，低 4 位为符号类型的
    /// </summary>
    public byte info { get; init; }

    /// <summary>
    ///     st_other 字段，通常的0的
    /// </summary>
    public byte other { get; init; }

    /// <summary>
    ///     关联的节区索引的
    /// </summary>
    public ushort section_index { get; init; }

    /// <summary>
    ///     符号值（地址或偏移）的
    /// </summary>
    public ulong value { get; init; }

    /// <summary>
    ///     符号大小（字节）的
    /// </summary>
    public ulong size { get; init; }

    /// <summary>
    ///     符号名称（解码后从字符串表填充）的
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     符号绑定类型（STB_LOCAL / STB_GLOBAL / STB_WEAK）的
    /// </summary>
    public byte bind => (byte)(info >> 4);

    /// <summary>
    ///     符号类型（STT_NOTYPE / STT_OBJECT / STT_FUNC / STT_SECTION / STT_FILE）的
    /// </summary>
    public byte type => (byte)(info & 0x0F);
}