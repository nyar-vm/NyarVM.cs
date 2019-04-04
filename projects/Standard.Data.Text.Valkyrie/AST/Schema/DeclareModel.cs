using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.Schema;

/// <summary>
///     数据模型声明，定义持久化数据的结构
/// </summary>
/// <para>示例：</para>
/// <code>
/// model User {
///     id: uuid;
///     name: utf8;
///     email: utf8;
/// }
/// model Order {
///     id: uuid,
///     seller_id: &User
///     sellee_id: &User
/// }
/// </code>
public sealed record DeclareModel : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; } = null;

    /// <summary>
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     模型名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}