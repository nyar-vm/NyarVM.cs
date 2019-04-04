using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>
///     Continue 语句
/// </summary>
public sealed record CContinue(TextSpan Span = default(TextSpan)) : CAstNode(Span);