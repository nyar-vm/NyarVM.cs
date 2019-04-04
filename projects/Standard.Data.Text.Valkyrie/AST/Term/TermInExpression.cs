using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     成员关系测试表达式 <c>expr in collection</c> 或 <c>expr in range</c> 或 <c>expr in type</c>
///     返回 bool，表示值是否属于集合、范围或类型
/// </summary>
public sealed record TermInExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermInExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermInExpression(ValkyrieNode operand, ValkyrieNode target, TextSpan span)
    {
        this.operand = operand;
        this.target = target;
        this.span = span;
    }

    /// <summary>
    ///     被测试的值
    /// </summary>
    public ValkyrieNode operand { get; init; } = null!;

    /// <summary>
    ///     目标集合、范围或类型
    /// </summary>
    public ValkyrieNode target { get; init; } = null!;

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.in_expr;
}