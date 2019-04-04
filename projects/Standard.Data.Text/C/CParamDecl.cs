using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     参数声明

/// 。</summary>
public sealed record CParamDecl(CAstNode type, string name, TextSpan Span = default(TextSpan))
    : CAstNode(Span);