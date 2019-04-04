using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin AST 基类
/// </summary>
/// <param name="span">源码位置。</param>
public abstract record JmAstNode(TextSpan span = default);