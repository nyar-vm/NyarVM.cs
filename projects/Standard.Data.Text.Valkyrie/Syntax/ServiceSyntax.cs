using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     服务声明语法节点：service Name { ... }
/// </summary>
public sealed class ServiceSyntax : ValkyrieDeclarationSyntax
{
    public ServiceSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>service 关键字。</summary>
    public SyntaxToken service_keyword => child_token(0);

    /// <summary>服务名称。</summary>
    public SyntaxToken name => child_token(1);

    /// <summary>左大括号。</summary>
    public SyntaxToken open_brace => child_token(2);

    /// <summary>右大括号。</summary>
    public SyntaxToken close_brace => last_token();
}