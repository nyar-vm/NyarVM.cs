using Std.Data.Text.Valkyrie.Parser;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     赋值语句，表示将右值写入左值目标。
/// </summary>
public sealed record AssignmentStatement : ValkyrieNode
{
    /// <summary>
    ///     赋值运算符；语法树中允许保留原始赋值形式。
    /// </summary>
    public TermBinaryOperator @operator { get; init; } = TermBinaryOperator.assign;

    /// <summary>
    ///     被写入的左值目标，如符号、字段或索引访问。
    /// </summary>
    public ValkyrieNode target { get; init; } = new IdentifierNode();

    /// <summary>
    ///     赋值右侧表达式。
    /// </summary>
    public ValkyrieNode value { get; init; } = new IdentifierNode();

    public override ValkyrieNodeType type => ValkyrieNodeType.assignment_statement;
}
