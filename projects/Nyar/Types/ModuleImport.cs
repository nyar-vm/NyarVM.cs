using Nyar.Analyzer.ModuleSystem;

namespace Nyar.Types;

/// <summary>
///     模块导入项
/// </summary>
public sealed class ModuleImport : IModuleImport
{
    /// <summary>
    ///     初始化 ModuleImport
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="symbolName">符号名称。</param>
    /// <param name="kind">导入类型。</param>
    public ModuleImport(string moduleName, string symbolName, ImportKind kind)
    {
        module_name = moduleName;
        symbol_name = symbolName;
        this.kind = kind;
    }

    /// <summary>
    ///     导入的模块名称
    /// </summary>
    public string module_name { get; init; }

    /// <summary>
    ///     导入的符号名称
    /// </summary>
    public string symbol_name { get; init; }

    /// <summary>
    ///     导入类型
    /// </summary>
    public ImportKind kind { get; init; }

    /// <summary>
    ///     返回类型（仅当 Kind 为 Function 时有效）
    /// </summary>
    public string? return_type { get; init; }

    /// <summary>
    ///     函数参数列表（仅当 Kind 为 Function 时有效）
    /// </summary>
    public List<FunctionParameter>? parameters { get; init; }

    /// <summary>
    ///     属性字典（如 js_builtin、pure 等标注）
    /// </summary>
    public IReadOnlyDictionary<string, string?>? attributes { get; init; }
}