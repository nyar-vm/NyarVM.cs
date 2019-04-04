using System.Collections.Generic;

namespace Std.Math.GraphTheory.Graph;

/// <summary>
///     泛型图接口，定义节点和边的增删查操作。
/// </summary>
/// <typeparam name="TNode">节点类型</typeparam>
/// <typeparam name="TEdge">边类型，推荐实现 <see cref="IEdge{TNode}"/> 以便使用图算法</typeparam>
public interface IGraph<TNode, TEdge> : IGraph
{
    /// <summary>
    ///     获取图中所有节点的列表。
    /// </summary>
    IReadOnlyList<TNode> Nodes { get; }

    /// <summary>
    ///     获取图中所有边的列表。
    /// </summary>
    IReadOnlyList<TEdge> Edges { get; }

    /// <summary>
    ///     向图中添加一个节点。重复节点会被忽略。
    /// </summary>
    /// <param name="node">要添加的节点</param>
    void AddNode(TNode node);

    /// <summary>
    ///     从图中移除指定节点及其关联的所有边。
    /// </summary>
    /// <param name="node">要移除的节点</param>
    /// <returns>成功移除返回 <c>true</c>，节点不存在返回 <c>false</c></returns>
    bool RemoveNode(TNode node);

    /// <summary>
    ///     向图中添加一条有向边。关联的源节点和目标节点会自动注册。
    /// </summary>
    /// <param name="edge">要添加的边</param>
    void AddEdge(TEdge edge);

    /// <summary>
    ///     从图中移除指定边。
    /// </summary>
    /// <param name="edge">要移除的边</param>
    /// <returns>成功移除返回 <c>true</c>，边不存在返回 <c>false</c></returns>
    bool RemoveEdge(TEdge edge);

    /// <summary>
    ///     获取指定节点的所有后继节点。
    /// </summary>
    /// <param name="node">源节点</param>
    /// <returns>后继节点列表</returns>
    IReadOnlyList<TNode> GetSuccessors(TNode node);

    /// <summary>
    ///     获取指定节点的所有前驱节点。
    /// </summary>
    /// <param name="node">目标节点</param>
    /// <returns>前驱节点列表</returns>
    IReadOnlyList<TNode> GetPredecessors(TNode node);

    /// <summary>
    ///     获取从指定节点出发的所有出边。
    /// </summary>
    /// <param name="node">源节点</param>
    /// <returns>出边列表</returns>
    IReadOnlyList<TEdge> GetOutEdges(TNode node);

    /// <summary>
    ///     获取指向指定节点的所有入边。
    /// </summary>
    /// <param name="node">目标节点</param>
    /// <returns>入边列表</returns>
    IReadOnlyList<TEdge> GetInEdges(TNode node);

    /// <summary>
    ///     检查图中是否包含指定节点。
    /// </summary>
    /// <param name="node">要检查的节点</param>
    /// <returns>存在返回 <c>true</c></returns>
    bool ContainsNode(TNode node);

    /// <summary>
    ///     检查图中是否包含指定边。
    /// </summary>
    /// <param name="edge">要检查的边</param>
    /// <returns>存在返回 <c>true</c></returns>
    bool ContainsEdge(TEdge edge);
}