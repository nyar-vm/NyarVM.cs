namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     CLR 目录表（PE 可选头中的 .NET 元数据入口点的2 字节）的
/// </summary>
public sealed class ClrDirectoryData
{
    /// <summary>
    ///     头部大小（字节）的
    /// </summary>
    public uint cb { get; init; }

    /// <summary>
    ///     主运行时版本号的
    /// </summary>
    public ushort major_runtime_version { get; init; }

    /// <summary>
    ///     次运行时版本号的
    /// </summary>
    public ushort minor_runtime_version { get; init; }

    /// <summary>
    ///     元数的RVA的
    /// </summary>
    public uint metadata_rva { get; init; }

    /// <summary>
    ///     元数据大小的
    /// </summary>
    public uint metadata_size { get; init; }

    /// <summary>
    ///     标志的see cref="ClrDirectoryFlags" />）的
    /// </summary>
    public uint flags { get; init; }

    /// <summary>
    ///     入口的RVA 或令牌的
    /// </summary>
    public uint entry_point { get; init; }

    /// <summary>
    ///     资源 RVA的
    /// </summary>
    public uint resources_rva { get; init; }

    /// <summary>
    ///     资源大小的
    /// </summary>
    public uint resources_size { get; init; }

    /// <summary>
    ///     强名称签的RVA的
    /// </summary>
    public uint strong_name_signature_rva { get; init; }

    /// <summary>
    ///     强名称签名大小的
    /// </summary>
    public uint strong_name_signature_size { get; init; }

    /// <summary>
    ///     代码管理器表 RVA的
    /// </summary>
    public uint code_manager_table_rva { get; init; }

    /// <summary>
    ///     代码管理器表大小的
    /// </summary>
    public uint code_manager_table_size { get; init; }

    /// <summary>
    ///     VTable 固定部分映射 RVA的
    /// </summary>
    public uint v_table_fixups_rva { get; init; }

    /// <summary>
    ///     VTable 固定部分映射大小的
    /// </summary>
    public uint v_table_fixups_size { get; init; }

    /// <summary>
    ///     导出地址的RVA的
    /// </summary>
    public uint export_address_table_jumps_rva { get; init; }

    /// <summary>
    ///     导出地址表大小的
    /// </summary>
    public uint export_address_table_jumps_size { get; init; }

    /// <summary>
    ///     托管原生的RVA的
    /// </summary>
    public uint managed_native_header_rva { get; init; }

    /// <summary>
    ///     托管原生头大小的
    /// </summary>
    public uint managed_native_header_size { get; init; }

    /// <summary>
    ///     是否为纯 IL 程序集的
    /// </summary>
    public bool is_il_only => (flags & (uint)ClrDirectoryFlags.il_only) != 0;

    /// <summary>
    ///     入口点是否为元数据令牌（而非 RVA）的
    /// </summary>
    public bool is_entry_point_token => (flags & (uint)ClrDirectoryFlags.native_entry_point) == 0;
}