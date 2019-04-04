using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     全局变量
/// </summary>
/// <param name="ValueType">值类型。</param>
/// <param name="IsMutable">是否可变。</param>
/// <param name="InitExpression">初始化表达式。</param>
/// <param name="Name">变量名。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatGlobal(
    string ValueType,
    bool IsMutable,
    List<WatInstruction> InitExpression,
    string? Name = null,
    TextSpan Span = default) : WatAstNode(Span);