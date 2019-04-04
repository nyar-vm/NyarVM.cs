using Nyar.Types.Targets;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler;

/// <summary>
///     代码生成后端接口（非泛型），提供后端元信息和编译能力。
/// </summary>
public interface ICodeGenBackend
{
    /// <summary>
    ///     后端名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     该后端支持的目标架构列表
    /// </summary>
    IReadOnlyList<TargetArch> supported_archs { get; }

    /// <summary>
    ///     编译 Nyar 模块到目标平台的输出规范
    /// </summary>
    /// <param name="module">要编译的 Nyar 模块。</param>
    /// <param name="options">编译选项。</param>
    /// <returns>输出规范，包含目标平台数据结构及其它编译产物。</returns>
    OutputSpec compile(GenerateModule module, CompilationOptions options);

    /// <summary>
    ///     验证 Nyar 模块是否能被此后端正确编译
    /// </summary>
    /// <param name="module">要验证的 Nyar 模块。</param>
    /// <param name="diagnostics">验证过程中产生的诊断信息列表。</param>
    /// <returns>验证是否通过。</returns>
    bool validate(GenerateModule module, out List<Diagnostic> diagnostics);
}

/// <summary>
///     代码生成后端接口（泛型），提供强类型输出版本的编译能力。
/// </summary>
/// <typeparam name="TOutput">目标平台数据结构类型</typeparam>
public interface ICodeGenBackend<TOutput> : ICodeGenBackend
{
    /// <summary>
    ///     编译 Nyar 模块到目标平台的强类型输出规范
    /// </summary>
    /// <param name="module">要编译的 Nyar 模块。</param>
    /// <param name="options">编译选项。</param>
    /// <returns>强类型输出规范。</returns>
    new OutputSpec<TOutput> compile(GenerateModule module, CompilationOptions options);
}