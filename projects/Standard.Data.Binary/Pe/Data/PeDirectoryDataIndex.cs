namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 数据目录索引（ECMA-335 的PE/COFF 标准定义）的
/// </summary>
public enum PeDirectoryDataIndex
{
    export_table = 0,
    import_table = 1,
    resource_table = 2,
    exception_table = 3,
    certificate_table = 4,
    base_relocation_table = 5,
    debug = 6,
    architecture = 7,
    global_ptr = 8,
    tls_table = 9,
    load_config_table = 10,
    bound_import = 11,
    iat = 12,
    delay_import_descriptor = 13,
    clr_runtime_header = 14,
    reserved = 15
}