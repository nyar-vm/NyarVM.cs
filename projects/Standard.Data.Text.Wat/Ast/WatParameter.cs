using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     函数参数
/// </summary>
/// <param name="ValueType">值类型。</param>
/// <param name="Name">参数名。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatParameter(string ValueType, string? Name = null, TextSpan Span = default) : WatAstNode(Span);