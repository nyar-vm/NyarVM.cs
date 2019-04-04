namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 文件头数据的
/// </summary>
public sealed class ElfHeaderData
{
    /// <summary>
    ///     魔数的x7F 'E' 'L' 'F'）的
    /// </summary>
    public byte[] magic { get; init; } = [];

    /// <summary>
    ///     类别的2的64位）的
    /// </summary>
    public byte @class { get; init; }

    /// <summary>
    ///     数据编码（小的大端）的
    /// </summary>
    public byte data_encoding { get; init; }

    /// <summary>
    ///     文件版本的
    /// </summary>
    public byte version { get; init; }

    /// <summary>
    ///     OS/ABI的
    /// </summary>
    public byte osabi { get; init; }

    /// <summary>
    ///     ABI 版本的
    /// </summary>
    public byte abi_version { get; init; }

    /// <summary>
    ///     文件类型的
    /// </summary>
    public ushort type { get; init; }

    /// <summary>
    ///     机器类型的
    /// </summary>
    public ushort machine { get; init; }

    /// <summary>
    ///     对象文件版本的
    /// </summary>
    public uint object_version { get; init; }

    /// <summary>
    ///     入口点地址的
    /// </summary>
    public ulong entry_point { get; init; }

    /// <summary>
    ///     程序头偏移的
    /// </summary>
    public ulong program_header_offset { get; init; }

    /// <summary>
    ///     节区头偏移的
    /// </summary>
    public ulong section_header_offset { get; init; }

    /// <summary>
    ///     标志的
    /// </summary>
    public uint flags { get; init; }

    /// <summary>
    ///     ELF 头大小的
    /// </summary>
    public ushort elf_header_size { get; init; }

    /// <summary>
    ///     程序头大小的
    /// </summary>
    public ushort program_header_size { get; init; }

    /// <summary>
    ///     程序头数量的
    /// </summary>
    public ushort program_header_count { get; init; }

    /// <summary>
    ///     节区头大小的
    /// </summary>
    public ushort section_header_size { get; init; }

    /// <summary>
    ///     节区头数量的
    /// </summary>
    public ushort section_header_count { get; init; }

    /// <summary>
    ///     字符串表节区索引的
    /// </summary>
    public ushort string_table_index { get; init; }

    /// <summary>
    ///     是否的64 位的
    /// </summary>
    public bool is64_bit => @class == ElfConstants.class64;

    /// <summary>
    ///     是否为小端的
    /// </summary>
    public bool is_little_endian => data_encoding == ElfConstants.data_encoding_little_endian;
}