using Std.Data.Text.Syntax;

namespace Std.Data.Text.Lexing;

/// <summary>
///     词法分析器接口，将源码文本转换为 Token 列表
/// </summary>
public interface ILexer
{
    /// <summary>
    ///     对源码文本进行词法分析，返回 Token 列表
    /// </summary>
    IReadOnlyList<GreenLeafNode> tokenize(string source);
}