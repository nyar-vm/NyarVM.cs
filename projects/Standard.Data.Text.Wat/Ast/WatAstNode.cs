using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     WAT AST 基类
/// </summary>
/// <param name="Span">源码位置。</param>
public abstract record WatAstNode(TextSpan Span = default);