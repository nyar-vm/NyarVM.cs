namespace Core.Compiler;

/// <summary>
///     编译器接口，提供从源代码到目标代码的完整编译能力
/// </summary>
public interface ICompiler
{
    /// <summary>
    ///     将源代码编译为目标平台代码
    /// </summary>
    /// <typeparam name="TTarget">目标平台的输出类型</typeparam>
    /// <param name="source">源代码文本</param>
    /// <param name="generator">目标平台的代码生成器</param>
    /// <returns>编译生成的目标代码</returns>
    TTarget compile<TTarget>(string source, ICodeGenerator<TTarget> generator);
}