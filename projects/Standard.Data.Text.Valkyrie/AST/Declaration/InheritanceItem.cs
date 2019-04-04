using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

public sealed record InheritanceItem : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     鍩虹被鎴栨帴鍙ｇ殑类型注解
    /// </summary>
    public TypeNode base_type { get; init; } = null!;

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    public IdentifierNode? name { get; init; } = null;
}