using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     `imply` 扩展块声明。
/// </summary>
/// <para>示例：</para>
/// <code>
/// imply Result&lt;T, E&gt; {
///     micro unwrap(self): T {
///         T.default
///     }
/// }
/// 
/// imply HashMap&lt;K, V&gt;: Dict&lt;K, V&gt; {
///     micro len(self): usize {
///         0
///     }
/// }
/// </code>
public sealed record DeclareImply : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     被扩展的目标类型
    /// </summary>
    public TypeNode target_type { get; init; } = null!;

    /// <summary>
    ///     承诺实现的抽象类型，可为空
    /// </summary>
    public TypeNode? contract_type { get; init; }

    /// <summary>
    ///     扩展块中的方法列表
    /// </summary>
    public IReadOnlyList<DeclareObjectMethod> methods { get; init; } = [];

    /// <summary>
    ///     扩展块中的关联类型绑定，如 <c>type Resume = i32</c>
    /// </summary>
    public IReadOnlyList<DeclareAssociatedType> associated_types { get; init; } = [];

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     声明名称，取扩展目标类型的名称
    /// </summary>
    public IdentifierNode? name => null;
}