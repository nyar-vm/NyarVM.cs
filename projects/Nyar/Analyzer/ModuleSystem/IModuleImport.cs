using Nyar.Types;

namespace Nyar.Analyzer.ModuleSystem;

/// <summary>
///     模块导入抽象接口
/// </summary>
public interface IModuleImport
{
    /// <summary>
    ///     导入的模块名称
    /// </summary>
    string module_name { get; }

    /// <summary>
    ///     导入的符号名称
    /// </summary>
    string symbol_name { get; }

    /// <summary>
    ///     导入类型
    /// </summary>
    ImportKind kind { get; }

    /// <summary>
    ///     返回类型（仅当 Kind 为 Function 时有效）
    /// </summary>
    string? return_type { get; }

    /// <summary>
    ///     函数参数列表（仅当 Kind 为 Function 时有效）
    /// </summary>
    List<FunctionParameter>? parameters { get; }

    /// <summary>
    ///     属性字典（如 js_builtin、pure 等标注）
    /// </summary>
    IReadOnlyDictionary<string, string?>? attributes { get; }
}