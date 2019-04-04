using Nyar.Types;

namespace Nyar.Analyzer.ModuleSystem;

/// <summary>
///     模块导出抽象接口
/// </summary>
public interface IModuleExport
{
    /// <summary>
    ///     导出名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     导出类型
    /// </summary>
    ExportKind kind { get; }

    /// <summary>
    ///     关联的函数索引（仅当 Kind 为 Function 时有效）
    /// </summary>
    int function_index { get; }
}