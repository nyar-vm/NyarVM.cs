using Nyar.Types;

namespace Nyar.Analyzer.ModuleSystem;

/// <summary>
///     模块抽象接口，统一运行时模块和编译期模块的表示
/// </summary>
public interface IModule
{
    /// <summary>
    ///     模块名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     模块版本号
    /// </summary>
    uint version { get; }

    /// <summary>
    ///     模块中的函数列表
    /// </summary>
    IReadOnlyList<IFunction> functions { get; }

    /// <summary>
    ///     模块的常量池
    /// </summary>
    IReadOnlyList<Value> constants { get; }

    /// <summary>
    ///     模块的导入列表
    /// </summary>
    IReadOnlyList<IModuleImport> imports { get; }

    /// <summary>
    ///     模块的导出列表
    /// </summary>
    IReadOnlyList<IModuleExport> exports { get; }

    /// <summary>
    ///     原始字节码（可选，用于调试和直接执行）
    /// </summary>
    byte[]? raw_bytecode { get; }

    /// <summary>
    ///     根据名称查找函数
    /// </summary>
    /// <param name="functionName">函数名称。</param>
    /// <returns>匹配的函数，未找到返回 null。</returns>
    IFunction? find_function(string functionName);

    /// <summary>
    ///     根据名称查找导出
    /// </summary>
    /// <param name="exportName">导出名称。</param>
    /// <returns>匹配的导出，未找到返回 null。</returns>
    IModuleExport? find_export(string exportName);
}