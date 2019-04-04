using System;
using System.Collections.Generic;
using System.Linq;

namespace Std.Math.GraphTheory.Graph;

public static partial class GraphAlgorithm
{
    /// <summary>
    ///     检测图中是否存在循环。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>存在循环返回 <c>true</c></returns>
    public static bool HasCycle<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();
        var recursionStack = new HashSet<TNode>();

        foreach (var node in graph.Nodes)
        {
            if (visited.Contains(node))
            {
                continue;
            }

            if (DfsDetectCycle(node, graph, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     在图中查找一个循环路径。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>循环路径节点列表，无环返回空列表</returns>
    public static IReadOnlyList<TNode> FindCycle<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var visited = new HashSet<TNode>();
        var path = new List<TNode>();

        foreach (var node in graph.Nodes)
        {
            if (visited.Contains(node))
            {
                continue;
            }

            var cycle = DfsFindCycle(node, graph, visited, path);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return [];
    }

    #region 私有辅助

    private static bool DfsDetectCycle<TNode, TEdge>(
        TNode node,
        IGraph<TNode, TEdge> graph,
        HashSet<TNode> visited,
        HashSet<TNode> recursionStack)
        where TEdge : IEdge<TNode>
    {
        if (recursionStack.Contains(node))
        {
            return true;
        }

        if (visited.Contains(node))
        {
            return false;
        }

        visited.Add(node);
        recursionStack.Add(node);

        foreach (var successor in graph.GetSuccessors(node))
        {
            if (DfsDetectCycle(successor, graph, visited, recursionStack))
            {
                return true;
            }
        }

        recursionStack.Remove(node);
        return false;
    }

    private static IReadOnlyList<TNode>? DfsFindCycle<TNode, TEdge>(
        TNode node,
        IGraph<TNode, TEdge> graph,
        HashSet<TNode> visited,
        List<TNode> path)
        where TEdge : IEdge<TNode>
    {
        if (path.Contains(node))
        {
            var cycleStart = path.IndexOf(node);
            var cycle = path.Skip(cycleStart).ToList();
            cycle.Add(node);
            return cycle;
        }

        if (visited.Contains(node))
        {
            return null;
        }

        visited.Add(node);
        path.Add(node);

        foreach (var successor in graph.GetSuccessors(node))
        {
            var cycle = DfsFindCycle(successor, graph, visited, path);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        path.RemoveAt(path.Count - 1);
        return null;
    }

    #endregion
}