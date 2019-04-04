using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     if 表达式，如 <c>if condition { then_expr } else { else_expr }</c>
///     对条件进行求值并返回其中一个分支的结果
/// </summary>
public sealed record TermIfExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermIfExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermIfExpression(TermNode condition, TermNode thenBranch, TermNode elseBranch, TextSpan span)
    {
        this.condition = condition;
        then_branch = thenBranch;
        else_branch = elseBranch;
        this.span = span;
    }

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.if_expr;

    /// <summary>
    ///     条件表达式
    /// </summary>
    public TermNode condition { get; init; } = default!;

    /// <summary>
    ///     条件为真时的分支表达式
    /// </summary>
    public TermNode then_branch { get; init; } = default!;

    /// <summary>
    ///     条件为假时的分支表达式
    /// </summary>
    public TermNode else_branch { get; init; } = default!;
}