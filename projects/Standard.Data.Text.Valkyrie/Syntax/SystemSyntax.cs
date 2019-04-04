using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     系统声明语法节点：system Name { ... }
/// </summary>
public sealed class SystemSyntax : ValkyrieDeclarationSyntax
{
    public SystemSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>system 关键字。</summary>
    public SyntaxToken system_keyword => child_token(0);

    /// <summary>系统名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>左大括号。</summary>
    public SyntaxToken open_brace => child_token(2);

    /// <summary>右大括号。</summary>
    public SyntaxToken close_brace => last_token();
}