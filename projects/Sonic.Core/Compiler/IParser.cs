using System.Collections.Generic;

namespace Core.Compiler;

/// <summary>
///     语法分析器接口，负责将词法单元序列解析为语法树
/// </summary>
public interface IParser
{
    /// <summary>
    ///     将词法单元列表解析为语法树
    /// </summary>
    /// <param name="tokens">词法单元只读列表</param>
    /// <returns>解析后的语法树</returns>
    ISyntaxTree parse(IReadOnlyList<IToken> tokens);
}