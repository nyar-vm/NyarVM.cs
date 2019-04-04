using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     变量声明


/// </summary>
public sealed record CVarDecl(CAstNode type, string name, CAstNode? initializer, TextSpan Span = default(TextSpan))
    : CAstNode(Span);