using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     存储声明语法节点：storage Name { ... }
/// </summary>
public sealed class StorageSyntax : ValkyrieDeclarationSyntax
{
    public StorageSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>storage 关键字。</summary>
    public SyntaxToken storage_keyword => child_token(0);

    /// <summary>存储名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>左大括号。</summary>
    public SyntaxToken open_brace => child_token(2);

    /// <summary>右大括号。</summary>
    public SyntaxToken close_brace => last_token();
}