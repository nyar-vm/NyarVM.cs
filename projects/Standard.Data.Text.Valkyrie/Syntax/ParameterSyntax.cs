using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     参数声明语法节点：Name: Type
/// </summary>
public sealed class ParameterSyntax : ValkyrieDeclarationSyntax
{
    public ParameterSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>参数名称。</summary>
    public SyntaxToken name => child_token(0);

    /// <summary>冒号。</summary>
    public SyntaxToken colon => child_token(1);

    /// <summary>类型名称。</summary>
    public SyntaxToken type_name => child_token(2);
}