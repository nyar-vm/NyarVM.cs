namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 导入条目的
/// </summary>
public sealed class NyarImport
{
    public NyarImport(NyarImportKind kind, string moduleName, string symbolName)
    {
        this.kind = kind;
        module_name = moduleName;
        symbol_name = symbolName;
    }

    /// <summary>
    ///     导入类型的
    /// </summary>
    public NyarImportKind kind { get; init; }

    /// <summary>
    ///     模块名称的
    /// </summary>
    public string module_name { get; init; }

    /// <summary>
    ///     符号名称的
    /// </summary>
    public string symbol_name { get; init; }
}