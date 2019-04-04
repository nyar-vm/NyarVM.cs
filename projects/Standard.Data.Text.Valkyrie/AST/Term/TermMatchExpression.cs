using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     后缀 match 表达式 <c>expr.match { case ...: ... }</c>
///     对表达式值进行多分支模式匹配
/// </summary>
public sealed record TermMatchExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermMatchExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermMatchExpression(TermNode operand, IReadOnlyList<ArmNode> arms, TextSpan span)
    {
        this.operand = operand;
        this.arms = arms;
        this.span = span;
    }

    /// <summary>
    ///     被匹配的表达式
    /// </summary>
    public TermNode operand { get; init; } = default!;

    /// <summary>
    ///     匹配分支列表
    /// </summary>
    public IReadOnlyList<ArmNode> arms { get; init; } = [];

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.match_expr;
}