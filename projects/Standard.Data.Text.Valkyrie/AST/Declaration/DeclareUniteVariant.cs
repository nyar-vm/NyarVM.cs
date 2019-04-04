namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     联合类型的单个变体声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// unite Result<T, E>
///         {
///         Success { value: T },
///         Failure { error: E },
///         }
/// </code>
public sealed record DeclareUniteVariant : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; } = null;

    /// <summary>
    ///     变体名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();
}