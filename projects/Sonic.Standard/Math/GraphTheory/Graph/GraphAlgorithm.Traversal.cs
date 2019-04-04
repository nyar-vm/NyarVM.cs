using System;
using System.Collections.Generic;

namespace Std.Math.GraphTheory.Graph;

public static partial class GraphAlgorithm
{
    /// <summary>
    ///     深度优先遍历，从指定起始节点开始。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <param name="startNode">起始节点</param>
    /// <returns>DFS 访问顺序的节点枚举</returns>
    public static IEnumerable<TNode> DepthFirstTraversal<TNode, TEdge>(
        this IGraph<TNode, TEdge> graph,
        TNode startNode)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();
        var stack = new Stack<TNode>();
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            yield return current;

            var successors = graph.GetSuccessors(current);
            for (int i = successors.Count - 1; i >= 0; i--)
            {
                var successor = successors[i];
                if (!visited.Contains(successor))
                {
                    stack.Push(successor);
                }
            }
        }
    }

    /// <summary>
    ///     深度优先遍历，从图中所有未访问节点开始。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>DFS 访问顺序的节点枚举</returns>
    public static IEnumerable<TNode> DepthFirstTraversal<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();

        foreach (var node in graph.Nodes)
        {
            if (visited.Contains(node))
            {
                continue;
            }

            foreach (var visitedNode in DepthFirstTraversalFrom(node, graph, visited))
            {
                yield return visitedNode;
            }
        }
    }

    /// <summary>
    ///     广度优先遍历，从指定起始节点开始。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <param name="startNode">起始节点</param>
    /// <returns>BFS 访问顺序的节点枚举</returns>
    public static IEnumerable<TNode> BreadthFirstTraversal<TNode, TEdge>(
        this IGraph<TNode, TEdge> graph,
        TNode startNode)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();
        var queue = new Queue<TNode>();
        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var successor in graph.GetSuccessors(current))
            {
                if (visited.Add(successor))
                {
                    queue.Enqueue(successor);
                }
            }
        }
    }

    /// <summary>
    ///     广度优先遍历，从图中所有未访问节点开始。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>BFS 访问顺序的节点枚举</returns>
    public static IEnumerable<TNode> BreadthFirstTraversal<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();

        foreach (var node in graph.Nodes)
        {
            if (visited.Contains(node))
            {
                continue;
            }

            foreach (var visitedNode in BreadthFirstTraversalFrom(node, graph, visited))
            {
                yield return visitedNode;
            }
        }
    }

    /// <summary>
    ///     后序遍历，所有后继访问完毕后才输出当前节点。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>后序遍历节点列表</returns>
    public static IReadOnlyList<TNode> PostorderTraversal<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var order = new List<TNode>();
        var visited = new HashSet<TNode>();

        foreach (var node in graph.Nodes)
        {
            TraversePostorder(node, graph, visited, order);
        }

        return order;
    }

    /// <summary>
    ///     逆后序遍历。先对图做后序遍历，再反转结果。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>逆后序遍历节点列表</returns>
    public static IReadOnlyList<TNode> ReversePostorderTraversal<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var postorder = PostorderTraversal(graph);
        var result = new List<TNode>(postorder);
        result.Reverse();
        return result;
    }

    #region 私有辅助

    private static IEnumerable<TNode> DepthFirstTraversalFrom<TNode, TEdge>(
        TNode startNode,
        IGraph<TNode, TEdge> graph,
        HashSet<TNode> visited)
        where TEdge : IEdge<TNode>
    {
        var stack = new Stack<TNode>();
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            yield return current;

            var successors = graph.GetSuccessors(current);
            for (int i = successors.Count - 1; i >= 0; i--)
            {
                var successor = successors[i];
                if (!visited.Contains(successor))
                {
                    stack.Push(successor);
                }
            }
        }
    }

    private static IEnumerable<TNode> BreadthFirstTraversalFrom<TNode, TEdge>(
        TNode startNode,
        IGraph<TNode, TEdge> graph,
        HashSet<TNode> visited)
        where TEdge : IEdge<TNode>
    {
        var queue = new Queue<TNode>();
        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var successor in graph.GetSuccessors(current))
            {
                if (visited.Add(successor))
                {
                    queue.Enqueue(successor);
                }
            }
        }
    }

    private static void TraversePostorder<TNode, TEdge>(
        TNode node,
        IGraph<TNode, TEdge> graph,
        HashSet<TNode> visited,
        List<TNode> order)
        where TEdge : IEdge<TNode>
    {
        if (!visited.Add(node))
        {
            return;
        }

        foreach (var successor in graph.GetSuccessors(node))
        {
            TraversePostorder(successor, graph, visited, order);
        }

        order.Add(node);
    }

    #endregion
}