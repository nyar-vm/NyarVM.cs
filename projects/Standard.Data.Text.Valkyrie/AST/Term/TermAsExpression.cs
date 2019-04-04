using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     显式类型转换表达式 <c>expr as T</c> 或 <c>expr as? T</c>
///     强制将值转换为目标类型，不安全转换会产生编译错误
/// </summary>
public sealed record TermAsExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermAsExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermAsExpression(ValkyrieNode operand, TypeNode targetType, bool isNullable, TextSpan span)
    {
        this.operand = operand;
        target_type = targetType;
        is_nullable = isNullable;
        this.span = span;
    }

    /// <summary>
    ///     被转换的表达式
    /// </summary>
    public ValkyrieNode operand { get; init; } = null!;

    /// <summary>
    ///     目标类型注解
    /// </summary>
    public TypeNode target_type { get; init; } = null!;

    /// <summary>
    ///     是否为可空转换（对应 <c>as?</c> 语法）
    ///     默认为 <c>false</c>
    /// </summary>
    public bool is_nullable { get; init; }

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.cast_expr;
}