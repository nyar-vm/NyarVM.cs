namespace Std.Data.Binary.Elf.Data;

/// <summary>
///     ELF 节区头数据的
/// </summary>
public sealed class ElfSectionHeaderData
{
    /// <summary>
    ///     节区名称字符串表索引的
    /// </summary>
    public uint name_index { get; init; }

    /// <summary>
    ///     节区名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     节区类型的
    /// </summary>
    public uint type { get; init; }

    /// <summary>
    ///     节区标志的
    /// </summary>
    public ulong flags { get; init; }

    /// <summary>
    ///     虚拟地址的
    /// </summary>
    public ulong address { get; init; }

    /// <summary>
    ///     文件偏移的
    /// </summary>
    public ulong offset { get; init; }

    /// <summary>
    ///     节区大小的
    /// </summary>
    public ulong size { get; init; }

    /// <summary>
    ///     链接索引的
    /// </summary>
    public uint link { get; init; }

    /// <summary>
    ///     附加信息的
    /// </summary>
    public uint info { get; init; }

    /// <summary>
    ///     对齐的
    /// </summary>
    public ulong alignment { get; init; }

    /// <summary>
    ///     条目大小的
    /// </summary>
    public ulong entry_size { get; init; }

    /// <summary>
    ///     节区原始内容数据的
    /// </summary>
    public byte[] content { get; set; } = [];
}