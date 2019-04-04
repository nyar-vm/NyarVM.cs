using System.Collections.Generic;

namespace Std.Math.GraphTheory.Graph;

public static partial class GraphAlgorithm
{
    /// <summary>
    ///     计算图中的强连通分量（Tarjan 算法）。
    /// </summary>
    /// <typeparam name="TNode">节点类型</typeparam>
    /// <typeparam name="TEdge">边类型</typeparam>
    /// <param name="graph">有向图</param>
    /// <returns>SCC 列表，每个 SCC 是一个节点集合</returns>
    public static IReadOnlyList<IReadOnlySet<TNode>> ComputeStronglyConnectedComponents<TNode, TEdge>(
        this IGraph<TNode, TEdge> graph)
        where TEdge : IEdge<TNode>
    {
        var indexCounter = 0;
        var stack = new Stack<TNode>();
        var indices = new Dictionary<TNode, int>();
        var lowLinks = new Dictionary<TNode, int>();
        var onStack = new HashSet<TNode>();
        var result = new List<IReadOnlySet<TNode>>();

        foreach (var node in graph.Nodes)
        {
            if (!indices.ContainsKey(node))
            {
                StrongConnect(node, graph, ref indexCounter, stack, indices, lowLinks, onStack, result);
            }
        }

        return result;
    }

    #region 私有辅助

    private static void StrongConnect<TNode, TEdge>(
        TNode node,
        IGraph<TNode, TEdge> graph,
        ref int indexCounter,
        Stack<TNode> stack,
        Dictionary<TNode, int> indices,
        Dictionary<TNode, int> lowLinks,
        HashSet<TNode> onStack,
        List<IReadOnlySet<TNode>> result)
        where TEdge : IEdge<TNode>
    {
        var index = indexCounter;
        indices[node] = index;
        lowLinks[node] = index;
        indexCounter++;
        stack.Push(node);
        onStack.Add(node);

        foreach (var successor in graph.GetSuccessors(node))
        {
            if (!indices.ContainsKey(successor))
            {
                StrongConnect(successor, graph, ref indexCounter, stack, indices, lowLinks, onStack, result);
                lowLinks[node] = Min(lowLinks[node], lowLinks[successor]);
            }
            else if (onStack.Contains(successor))
            {
                lowLinks[node] = Min(lowLinks[node], indices[successor]);
            }
        }

        if (lowLinks[node] == index)
        {
            var scc = new HashSet<TNode>();
            TNode w;
            do
            {
                w = stack.Pop();
                onStack.Remove(w);
                scc.Add(w);
            } while (!EqualityComparer<TNode>.Default.Equals(w, node));

            result.Add(scc);
        }
    }

    private static int Min(int a, int b)
    {
        return a < b ? a : b;
    }

    #endregion
}