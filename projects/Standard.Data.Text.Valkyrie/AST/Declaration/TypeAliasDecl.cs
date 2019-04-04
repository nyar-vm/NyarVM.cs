using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     类型别名声明节点，如 <c>typealias Age = i32</c>
/// </summary>
public sealed record TypeAliasDecl : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     目标类型注解
    /// </summary>
    public TypeNode target_type { get; init; } = new TypeLiteralNamePathNode();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     别名名称
    /// </summary>
    public IdentifierNode? name { get; init; }
}