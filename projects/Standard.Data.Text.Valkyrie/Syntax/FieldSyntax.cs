using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     字段声明语法节点：Name: Type (= defaultValue)?
/// </summary>
public sealed class FieldSyntax : ValkyrieDeclarationSyntax
{
    public FieldSyntax(GreenNode green, SyntaxTree tree, int offset) : base(green, tree, offset)
    {
    }

    /// <summary>字段名称。</summary>
    public SyntaxToken name => child_token(0);

    /// <summary>冒号。</summary>
    public SyntaxToken colon => child_token(1);

    /// <summary>类型名称。</summary>
    public SyntaxToken type_name => child_token(2);

    /// <summary>是否有默认值。</summary>
    public bool has_default_value => child_count >= 5;

    /// <summary>等号（仅在 HasDefaultValue 时有效）。</summary>
    public SyntaxToken? equals_token => has_default_value ? child_token(3) : null;

    /// <summary>默认值（仅在 HasDefaultValue 时有效）。</summary>
    public SyntaxToken? default_value => has_default_value ? child_token(4) : null;
}