using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>
///     函数调用表达式
/// </summary>
public sealed record CCall(CAstNode function, IReadOnlyList<CAstNode> arguments, TextSpan Span = default(TextSpan)) : CAstNode(Span);