using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     变量声明语法节点：let/var Name: Type (= value)?
/// </summary>
public sealed class VariableSyntax : ValkyrieDeclarationSyntax
{
    public VariableSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>let 或 var 关键字。</summary>
    public SyntaxToken let_keyword => child_token(0);

    /// <summary>变量名称。</summary>
    public SyntaxToken name => child_token(1);
}