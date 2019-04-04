using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     标识符表达式


/// </summary>
public sealed record CIdentifier(string name, TextSpan Span = default(TextSpan))
    : CAstNode(Span);