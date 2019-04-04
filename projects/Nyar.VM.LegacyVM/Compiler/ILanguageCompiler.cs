using Nyar.Assembler;

namespace Nyar.VM.LegacyVM.Compiler;

/// <summary>
///     语言编译器接口，将源码编译为 <see cref="GenerateModule" /> 中间表示。
/// </summary>
public interface ILanguageCompiler
{
    /// <summary>
    ///     语言标识
    /// </summary>
    string language { get; }

    /// <summary>
    ///     编译源码为元编译模块
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="moduleName">模块名称</param>
    /// <returns>编译后的元编译模块</returns>
    GenerateModule compile(string source, string moduleName);
}