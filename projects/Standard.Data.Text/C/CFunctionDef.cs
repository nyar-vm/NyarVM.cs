using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     函数定义

/// 。</summary>
public sealed record CFunctionDef(
    CAstNode return_type,
    string name,
    IReadOnlyList<CParamDecl> parameters,
    CAstNode body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);