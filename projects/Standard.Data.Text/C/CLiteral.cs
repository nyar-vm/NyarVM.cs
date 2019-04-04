using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     字面量表达式


/// </summary>
public sealed record CLiteral(string kind, string value, TextSpan Span = default(TextSpan))
    : CAstNode(Span);