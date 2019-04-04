using Nyar.Analyzer.ModuleSystem;

namespace Nyar.Types;

/// <summary>
///     模块导出项
/// </summary>
public sealed class ModuleExport : IModuleExport
{
    /// <summary>
    ///     初始化 ModuleExport
    /// </summary>
    /// <param name="name">导出名称。</param>
    /// <param name="kind">导出类型。</param>
    /// <param name="functionIndex">函数索引。</param>
    public ModuleExport(string name, ExportKind kind, int functionIndex)
    {
        this.name = name;
        this.kind = kind;
        function_index = functionIndex;
    }

    /// <summary>
    ///     导出名称
    /// </summary>
    public string name { get; init; }

    /// <summary>
    ///     导出类型
    /// </summary>
    public ExportKind kind { get; init; }

    /// <summary>
    ///     关联的函数索引（仅当 Kind 为 Function 时有效）
    /// </summary>
    public int function_index { get; init; }
}