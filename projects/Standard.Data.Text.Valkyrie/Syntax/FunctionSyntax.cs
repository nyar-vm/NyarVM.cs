using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     函数声明语法节点：micro Name(...) -> Type { ... }
/// </summary>
public sealed class FunctionSyntax : ValkyrieDeclarationSyntax
{
    public FunctionSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>micro 关键字。</summary>
    public SyntaxToken function_keyword => child_token(0);

    /// <summary>函数名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>参数列表。</summary>
    public IReadOnlyList<ParameterSyntax> parameters => collect_children<ParameterSyntax>();
}