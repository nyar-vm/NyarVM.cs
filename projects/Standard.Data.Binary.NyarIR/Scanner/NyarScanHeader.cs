namespace Std.Data.Binary.NyarIR.Scanner;

/// <summary>
///     Nyar 扫描头部信息的
/// </summary>
public sealed class NyarScanHeader
{
    /// <summary>
    ///     版本号的
    /// </summary>
    public uint version { get; set; }

    /// <summary>
    ///     模块名称的
    /// </summary>
    public string module_name { get; set; } = string.Empty;

    /// <summary>
    ///     段数量的
    /// </summary>
    public int section_count { get; set; }

    /// <summary>
    ///     段概要信息列表的
    /// </summary>
    public IReadOnlyList<NyarSectionScanInfo> sections { get; set; } = [];

    /// <summary>
    ///     是否包含常量池段的
    /// </summary>
    public bool has_constants_section { get; set; }

    /// <summary>
    ///     是否包含函数表段的
    /// </summary>
    public bool has_functions_section { get; set; }

    /// <summary>
    ///     是否包含导入表段的
    /// </summary>
    public bool has_imports_section { get; set; }

    /// <summary>
    ///     是否包含导出表段的
    /// </summary>
    public bool has_exports_section { get; set; }

    /// <summary>
    ///     是否包含代码段的
    /// </summary>
    public bool has_code_section { get; set; }
}