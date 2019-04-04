using Core.Compiler.Token;

namespace Core.Compiler;

/// <summary>
///     词法单元接口，表示词法分析产生的最小语法单位
/// </summary>
public interface IToken
{
    /// <summary>
    ///     获取词法单元的类别
    /// </summary>
    TokenKind kind { get; }

    /// <summary>
    ///     获取词法单元的原始文本
    /// </summary>
    string text { get; }
}