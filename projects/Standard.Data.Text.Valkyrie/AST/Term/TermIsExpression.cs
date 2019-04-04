using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     类型测试表达式 <c>x is T</c> 或可空类型测试 <c>x is T?</c>
///     返回 <see langword="bool" />，表示值是否为指定类型
/// </summary>
public sealed record TermIsExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermIsExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermIsExpression(ValkyrieNode operand, PatternNode targetPatternNode, TextSpan span, bool isNullable = false)
    {
        this.operand = operand;
        target_pattern_node = targetPatternNode;
        this.span = span;
        is_nullable = isNullable;
    }

    /// <summary>
    ///     被测试的表达式
    /// </summary>
    public ValkyrieNode operand { get; init; } = default!;

    /// <summary>
    ///     目标类型模式
    /// </summary>
    public PatternNode target_pattern_node { get; init; } = null!;

    /// <summary>
    ///     是否测试可空类型，如 <c>x is T?</c>
    /// </summary>
    public bool is_nullable { get; init; }

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.is_expr;
}