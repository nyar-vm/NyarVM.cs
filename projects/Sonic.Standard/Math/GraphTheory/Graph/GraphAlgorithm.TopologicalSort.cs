using System;
using System.Collections.Generic;

namespace Std.Math.GraphTheory.Graph;

/// <summary>
///     图算法静态类，提供有向图通用算法的扩展方法。
/// </summary>
public static partial class GraphAlgorithm
{
    /// <summary>
    ///     对图进行拓扑排序（Kahn 算法）。
    ///     入度为零的节点按添加顺序排列。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>按拓扑序排列的节点列表</returns>
    /// <exception cref="InvalidOperationException">图中存在循环依赖</exception>
    public static IReadOnlyList<TNode> TopologicalSort<TNode, TEdge>(this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var inDegree = new Dictionary<TNode, int>();

        foreach (var node in graph.Nodes)
        {
            inDegree[node] = 0;
        }

        foreach (var node in graph.Nodes)
        {
            foreach (var successor in graph.GetSuccessors(node))
            {
                if (inDegree.ContainsKey(successor))
                {
                    inDegree[successor]++;
                }
            }
        }

        var queue = new Queue<TNode>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        var result = new List<TNode>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var successor in graph.GetSuccessors(current))
            {
                inDegree[successor]--;
                if (inDegree[successor] == 0)
                {
                    queue.Enqueue(successor);
                }
            }
        }

        if (result.Count != graph.NodeCount)
        {
            throw new InvalidOperationException("图中存在循环依赖，无法进行拓扑排序");
        }

        return result;
    }
}