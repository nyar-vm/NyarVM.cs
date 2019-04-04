using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     类型定义（typedef）

/// 。</summary>
public sealed record CTypedef(CAstNode type, string name, TextSpan Span = default(TextSpan))
    : CAstNode(Span);