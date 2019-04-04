namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 导出条目的
/// </summary>
public sealed class NyarExport
{
    public NyarExport(NyarExportKind kind, string symbolName, int functionIndex)
    {
        this.kind = kind;
        symbol_name = symbolName;
        function_index = functionIndex;
    }

    /// <summary>
    ///     导出类型的
    /// </summary>
    public NyarExportKind kind { get; init; }

    /// <summary>
    ///     符号名称的
    /// </summary>
    public string symbol_name { get; init; } = string.Empty;

    /// <summary>
    ///     关联的函数索引（仅当 Kind 的Function 时有效）的
    /// </summary>
    public int function_index { get; init; }
}