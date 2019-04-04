using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.ECS;

/// <summary>
///     ECS 系统声明，定义对满足条件的实体集合执行的操作逻辑
/// </summary>
/// <para>示例：</para>
/// <code>
/// system movement {
///     query all [Position, Velocity];
/// 
///     micro run(param: f32) {
///         for each entity {
///             entity.position += entity.velocity * param;
///         }
///     }
/// }
/// </code>
public sealed record DeclareSystem : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     查询表达式列表，定义系统操作的实体范围
    /// </summary>
    public IReadOnlyList<QueryExpr> queries { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; } = null;

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     系统名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}