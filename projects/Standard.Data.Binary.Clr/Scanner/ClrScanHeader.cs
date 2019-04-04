namespace Std.Data.Binary.Clr.Scanner;

/// <summary>
///     CLR 扫描头部信息
/// </summary>
public sealed class ClrScanHeader
{
    /// <summary>
    ///     目标机器类型的    ///。
    /// </summary>
    public ushort machine { get; set; }

    /// <summary>
    ///     节区数量的    ///。
    /// </summary>
    public ushort number_of_sections { get; set; }

    /// <summary>
    ///     是否的PE32+ 格式的4 位）的    ///。
    /// </summary>
    public bool is_pe32_plus { get; set; }

    /// <summary>
    ///     是否包含 CLR 目录的    ///。
    /// </summary>
    public bool has_clr_directory { get; set; }

    /// <summary>
    ///     是否为可执行文件的    ///。
    /// </summary>
    public bool is_executable { get; set; }

    /// <summary>
    ///     是否的DLL的    ///。
    /// </summary>
    public bool is_dll { get; set; }

    /// <summary>
    ///     CLR 元数的RVA的    ///。
    /// </summary>
    public uint clr_metadata_rva { get; set; }

    /// <summary>
    ///     CLR 元数据大小的    ///。
    /// </summary>
    public uint clr_metadata_size { get; set; }
}