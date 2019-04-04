using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     成员访问表达式


/// </summary>
public sealed record CMemberAccess(CAstNode @object, string member, bool is_pointer, TextSpan Span = default(TextSpan))
    : CAstNode(Span);