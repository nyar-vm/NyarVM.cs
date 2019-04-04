using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     类型定义
/// </summary>
/// <param name="Parameters">参数类型列表。</param>
/// <param name="Results">返回值类型列表。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatTypeDefinition(List<string> Parameters, List<string> Results, TextSpan Span = default)
    : WatAstNode(Span);