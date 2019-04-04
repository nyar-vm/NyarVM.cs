using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     导入声明
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Module">模块名。</param>
/// <param name="Field">字段名。</param>
/// <param name="Descriptor">导入描述。</param>
public sealed record WatImport(TextSpan Span, string Module, string Field, WatAstNode Descriptor) : WatAstNode(Span);