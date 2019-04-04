using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     ILASM AST 基类
/// </summary>
/// <param name="span">源码位置。</param>
public abstract record MsilAstNode(TextSpan span = default);