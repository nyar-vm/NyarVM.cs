using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     函数参数项声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro f(p) { ... }
/// micro f(p: Type) { ... }
/// micro f(p: Type = default) { ... }
/// </code>
public sealed record TermParameterItem : ValkyrieNode, IDeclarationNode
{
    public TermParameterItem(IdentifierNode name, TypeNode? boundType, TermNode? defaultTerm)
    {
        this.name = name;
        bound_type = boundType;
        default_term = defaultTerm;
    }

    /// <summary>
    ///     参数类型注解
    /// </summary>
    public TypeNode? bound_type { get; init; }

    /// <summary>
    ///     默认值表达式
    /// </summary>
    public TermNode? default_term { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     参数名称
    /// </summary>
    public IdentifierNode? name { get; init; }
}