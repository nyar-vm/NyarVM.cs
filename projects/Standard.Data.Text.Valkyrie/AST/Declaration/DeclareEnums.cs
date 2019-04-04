namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     枚举声明
/// </summary>
public sealed record DeclareEnums : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     枚举成员列表
    /// </summary>
    public IReadOnlyList<DeclareSemanticMember> members { get; init; } = [];

    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     枚举名称
    /// </summary>
    public IdentifierNode name { get; init; } = new();
}