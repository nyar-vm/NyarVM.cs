namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 程序头数据的
/// </summary>
public sealed class ElfProgramHeaderData
{
    /// <summary>
    ///     段类型的
    /// </summary>
    public uint type { get; init; }

    /// <summary>
    ///     段标志的
    /// </summary>
    public uint flags { get; init; }

    /// <summary>
    ///     文件偏移的
    /// </summary>
    public ulong offset { get; init; }

    /// <summary>
    ///     虚拟地址的
    /// </summary>
    public ulong virtual_address { get; init; }

    /// <summary>
    ///     物理地址的
    /// </summary>
    public ulong physical_address { get; init; }

    /// <summary>
    ///     文件大小的
    /// </summary>
    public ulong file_size { get; init; }

    /// <summary>
    ///     内存大小的
    /// </summary>
    public ulong memory_size { get; init; }

    /// <summary>
    ///     对齐的
    /// </summary>
    public ulong alignment { get; init; }
}