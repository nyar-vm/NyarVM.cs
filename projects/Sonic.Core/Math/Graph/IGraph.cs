namespace Core.Math.Graph;

/// <summary>
///     图接口，定义节点和边的添加与计数方式。
/// </summary>
/// <typeparam name="TNode">节点的类型。</typeparam>
/// <typeparam name="TEdge">边的类型。</typeparam>
public interface IGraph<TNode, TEdge>
{
    /// <summary>
    ///     获取图中节点的数量。
    /// </summary>
    int node_count { get; }

    /// <summary>
    ///     获取图中边的数量。
    /// </summary>
    int edge_count { get; }

    /// <summary>
    ///     向图中添加一个节点。
    /// </summary>
    /// <param name="node">要添加的节点。</param>
    void add_node(TNode node);

    /// <summary>
    ///     向图中添加一条边。
    /// </summary>
    /// <param name="edge">要添加的边。</param>
    void add_edge(TEdge edge);
}