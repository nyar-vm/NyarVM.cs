using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     局部变量
/// </summary>
/// <param name="ValueType">值类型。</param>
/// <param name="Name">变量名。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatLocal(string ValueType, string? Name = null, TextSpan Span = default) : WatAstNode(Span);