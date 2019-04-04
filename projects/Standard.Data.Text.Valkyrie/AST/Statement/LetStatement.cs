using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     let / discard 语句占位节点，用于在语法层面忽略值
/// </summary>
public sealed record LetStatement : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public LetStatement()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public LetStatement(TextSpan span)
    {
        this.span = span;
    }

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.discard_stmt;
}