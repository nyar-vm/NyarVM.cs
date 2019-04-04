using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     后缀 catch 表达式 <c>expr.catch { case ...: ... }</c>
///     捕获表达式中可能抛出的效应操作并按模式匹配处理，可调用 <c>resume</c> 恢复
/// </summary>
public sealed record TermCatchExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermCatchExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermCatchExpression(TermNode operand, IReadOnlyList<ArmNode> arms, TextSpan span)
    {
        this.operand = operand;
        this.arms = arms;
        this.span = span;
    }

    /// <summary>
    ///     被监视的表达式
    /// </summary>
    public TermNode operand { get; init; } = default!;

    /// <summary>
    ///     catch 处理分支列表
    /// </summary>
    public IReadOnlyList<ArmNode> arms { get; init; } = [];

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.catch_expr;
}