using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     命名空间声明语法节点：namespace Name { ... }
/// </summary>
public sealed class NamespaceSyntax : ValkyrieDeclarationSyntax
{
    public NamespaceSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>namespace 关键字。</summary>
    public SyntaxToken namespace_keyword => child_token(0);

    /// <summary>命名空间名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>左大括号。</summary>
    public SyntaxToken open_brace => child_token(2);

    /// <summary>右大括号。</summary>
    public SyntaxToken close_brace => last_token();
}