using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     查询声明语法节点，用于 ECS 系统查询
/// </summary>
public sealed class QuerySyntax : ValkyrieDeclarationSyntax
{
    public QuerySyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>query 关键字。</summary>
    public SyntaxToken query_keyword => child_token(0);

    /// <summary>查询目标名称。</summary>
    public SyntaxToken name => child_token(1);
}