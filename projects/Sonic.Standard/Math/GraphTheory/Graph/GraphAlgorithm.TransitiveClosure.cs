using System.Collections.Generic;

namespace Std.Math.GraphTheory.Graph;

public static partial class GraphAlgorithm
{
    /// <summary>
    ///     获取从指定节点出发可达的所有节点（传递后继闭包）。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <param name="node">源节点</param>
    /// <returns>所有可达后继节点集合</returns>
    public static IReadOnlySet<TNode> GetTransitiveSuccessors<TNode, TEdge>(
        this IGraph<TNode, TEdge> graph,
        TNode node)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();
        var stack = new Stack<TNode>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var successor in graph.GetSuccessors(current))
            {
                if (visited.Add(successor))
                {
                    stack.Push(successor);
                }
            }
        }

        return visited;
    }

    /// <summary>
    ///     获取可以到达指定节点的所有节点（传递前驱闭包）。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <param name="node">目标节点</param>
    /// <returns>所有可达前驱节点集合</returns>
    public static IReadOnlySet<TNode> GetTransitivePredecessors<TNode, TEdge>(
        this IGraph<TNode, TEdge> graph,
        TNode node)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();
        var stack = new Stack<TNode>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var predecessor in graph.GetPredecessors(current))
            {
                if (visited.Add(predecessor))
                {
                    stack.Push(predecessor);
                }
            }
        }

        return visited;
    }
}