using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     枚举声明语法节点：enum Name { ... }
/// </summary>
public sealed class EnumSyntax : ValkyrieDeclarationSyntax
{
    public EnumSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>enum 关键字。</summary>
    public SyntaxToken enum_keyword => child_token(0);

    /// <summary>枚举名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>左大括号。</summary>
    public SyntaxToken open_brace => child_token(2);

    /// <summary>右大括号。</summary>
    public SyntaxToken close_brace => last_token();
}