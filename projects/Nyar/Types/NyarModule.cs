using Nyar.Analyzer.ModuleSystem;

namespace Nyar.Types;

/// <summary>
///     Nyar 字节码模块，封装编译后的代码和元数据
/// </summary>
public sealed class NyarModule : IModule
{
    /// <summary>
    ///     初始化 NyarModule
    /// </summary>
    /// <param name="name">模块名称。</param>
    public NyarModule(string name)
    {
        this.name = name;
        version = 1;
        constants = [];
        functions = [];
        imports = [];
        exports = [];
    }

    /// <summary>
    ///     常量池，存储字面量和引用常量
    /// </summary>
    public List<Value> constants { get; init; }

    /// <summary>
    ///     函数表，存储模块内定义的所有函数
    /// </summary>
    public List<NyarFunction> functions { get; init; }

    /// <summary>
    ///     导入表，存储模块依赖的外部符号
    /// </summary>
    public List<ModuleImport> imports { get; init; }

    /// <summary>
    ///     导出表，存储模块对外暴露的符号
    /// </summary>
    public List<ModuleExport> exports { get; init; }

    /// <summary>
    ///     Witness 分派条目列表，存储编译期生成的接口分派信息
    ///     在模块加载时注册到运行时 WitnessTable
    /// </summary>
    public List<WitnessDispatchEntry> witness_entries { get; init; } = [];

    /// <summary>
    ///     原生函数表，存储模块注册的原生函数实现
    /// </summary>
    public List<NyarNativeFunction> native_functions { get; init; } = [];

    /// <summary>
    ///     模块名称
    /// </summary>
    public string name { get; init; }

    /// <summary>
    ///     模块版本号
    /// </summary>
    public uint version { get; init; }

    /// <summary>
    ///     原始字节码（可选，用于调试）
    /// </summary>
    public byte[]? raw_bytecode { get; set; }

    /// <summary>
    ///     接口层常量池访问
    /// </summary>
    IReadOnlyList<Value> IModule.constants => constants;

    /// <summary>
    ///     接口层函数列表访问
    /// </summary>
    IReadOnlyList<IFunction> IModule.functions => functions;

    /// <summary>
    ///     接口层导入列表访问
    /// </summary>
    IReadOnlyList<IModuleImport> IModule.imports => imports;

    /// <summary>
    ///     接口层导出列表访问
    /// </summary>
    IReadOnlyList<IModuleExport> IModule.exports => exports;

    /// <summary>
    ///     接口层查找函数
    /// </summary>
    IFunction? IModule.find_function(string functionName)
    {
        return find_function(functionName);
    }

    /// <summary>
    ///     接口层查找导出
    /// </summary>
    IModuleExport? IModule.find_export(string exportName)
    {
        return find_export(exportName);
    }

    /// <summary>
    ///     根据名称查找函数
    /// </summary>
    /// <param name="functionName">函数名称。</param>
    /// <returns>匹配的函数，未找到返回 null。</returns>
    public NyarFunction? find_function(string functionName)
    {
        foreach (var func in functions)
            if (func.name == functionName)
                return func;

        return null;
    }

    /// <summary>
    ///     根据名称查找导出
    /// </summary>
    /// <param name="exportName">导出名称。</param>
    /// <returns>匹配的导出，未找到返回 null。</returns>
    public ModuleExport? find_export(string exportName)
    {
        foreach (var export in exports)
            if (export.name == exportName)
                return export;

        return null;
    }

    /// <summary>
    ///     根据名称查找原生函数
    /// </summary>
    /// <param name="functionName">函数名称。</param>
    /// <returns>匹配的原生函数，未找到返回 null。</returns>
    public NyarNativeFunction? find_native_function(string functionName)
    {
        foreach (var func in native_functions)
            if (func.name == functionName)
                return func;

        return null;
    }
}