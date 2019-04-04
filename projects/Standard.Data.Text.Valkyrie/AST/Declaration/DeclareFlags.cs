namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     位标志枚举声明
/// </summary>
public sealed record DeclareFlags : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     标志成员列表
    /// </summary>
    public IReadOnlyList<DeclareSemanticMember> members { get; init; } = [];

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     标志枚举名称
    /// </summary>
    public IdentifierNode name { get; init; } = new();
}