using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     WAT start 函数声明
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Function">函数名。</param>
public sealed record WatStart(TextSpan Span, string Function) : WatAstNode(Span);