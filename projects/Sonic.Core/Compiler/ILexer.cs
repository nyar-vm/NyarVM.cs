using System.Collections.Generic;

namespace Core.Compiler;

/// <summary>
///     词法分析器接口，负责将源代码文本转换为词法单元序列
/// </summary>
public interface ILexer
{
    /// <summary>
    ///     异步地将源代码文本转换为词法单元序列
    /// </summary>
    /// <param name="source">源代码文本</param>
    /// <returns>词法单元的异步可枚举序列</returns>
    IAsyncEnumerable<IToken> tokenize(string source);
}