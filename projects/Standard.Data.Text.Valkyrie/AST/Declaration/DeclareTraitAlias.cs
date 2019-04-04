using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     类型别名声明，为现有类型创建新名称
///     <para>示例：</para>
///     <code>
/// trait PlayerId = int;
/// trait Callback = { to_float(i32) -> f32 };
/// </code>
public sealed record DeclareTraitAlias : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     鐩爣绫诲瀷
    /// </summary>
    public TypeNode target_type { get; init; } = null!;

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}