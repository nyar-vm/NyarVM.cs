using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.ECS;

/// <summary>
///     ECS 查询表达式，用于筛选满足特定条件的实体集合
/// </summary>
/// <para>示例：</para>
/// <code>
/// // 查询同时拥有 Position 和 Velocity 组件的实体
/// system movement {
///     query all [Position, Velocity];
///     ...
/// }
/// 
/// // 查询拥有 Position 且没有任何 Dead 或 Paused 组件的实体
/// query all [Position]
///     none [Dead, Paused];
/// </code>
public sealed record QueryExpr : ValkyrieNode
{
    /// <summary>
    ///     查询类型（<c>all</c>、<c>any</c>、<c>none</c>）
    /// </summary>
    public QueryKind kind { get; init; }

    /// <summary>
    ///     查询涉及的组件类型列表
    /// </summary>
    public IReadOnlyList<TypeNode> component_types { get; init; } = [];

    /// <summary>
    ///     附加的子查询过滤器列表（如 <c>none [Dead]</c>）
    /// </summary>
    public IReadOnlyList<QueryExpr>? filters { get; init; }
}