using Nyar.Types.Targets;

namespace Nyar.Assembler;

/// <summary>
///     元编译选项。
/// </summary>
public sealed class CompilationOptions
{
    /// <summary>
    ///     目标编译三元组
    /// </summary>
    public CompilationTarget? target { get; set; }


    /// <summary>
    ///     优化级别
    /// </summary>
    public OptimizationLevel optimization_level { get; set; } = OptimizationLevel.basic;


    /// <summary>
    ///     是否生成 WebAssembly 文本格式 (.wat)
    /// </summary>
    public bool generate_wat { get; set; }


    /// <summary>
    ///     是否生成调试源映射 (Source Map)
    /// </summary>
    public bool generate_source_map { get; set; }


    /// <summary>
    ///     是否生成 TypeScript 类型定义 (.d.ts)
    /// </summary>
    public bool generate_type_script_decls { get; set; }


    /// <summary>
    ///     是否生成 MSIL 文本输出 (.msil)
    /// </summary>
    public bool generate_msil { get; set; }

    /// <summary>
    ///     多入口模块时，指定使用哪一个入口函数名作为编译入口。
    ///     为空时由后端自行决策（如查找 "main" 或第一个导出函数）。
    /// </summary>
    public string? entry_function_name { get; set; }
}