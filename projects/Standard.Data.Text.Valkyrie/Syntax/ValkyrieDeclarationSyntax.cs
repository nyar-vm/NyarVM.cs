using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 声明语法节点抽象基类
/// </summary>
public abstract class ValkyrieDeclarationSyntax : ValkyrieSyntaxNode
{
    /// <summary>
    ///     初始化声明语法节点
    /// </summary>
    protected ValkyrieDeclarationSyntax(GreenNode green, SyntaxTree tree, int offset)
        : base(green, tree, offset)
    {
    }
}