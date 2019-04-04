using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     Shader 声明语法节点：shader Name { ... }
/// </summary>
public sealed class ShaderSyntax : ValkyrieDeclarationSyntax
{
    public ShaderSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>shader 关键字。</summary>
    public SyntaxToken shader_keyword => child_token(0);

    /// <summary>Shader 名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>左大括号。</summary>
    public SyntaxToken open_brace => child_token(2);

    /// <summary>右大括号。</summary>
    public SyntaxToken close_brace => last_token();
}