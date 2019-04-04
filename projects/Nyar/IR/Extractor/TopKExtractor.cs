using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.IR.Extractor;

/// <summary>
///     Top-K 提取器扩展，为优化管线提供 Top-K 提取能力。
///     使用 BeamSearch 策略，对齐 Extractor 的 BeamSearch 算法，
///     通过迭代精炼得出各等价类的最优 K 个候选节点。
/// </summary>
public static class TopKExtractor
{
    /// <summary>
    ///     从 EGraph 中提取成本最优的前 K 个 Oa 节点
    /// </summary>
    /// <param name="egraph">目标 EGraph。</param>
    /// <param name="root">根等价类 Id。</param>
    /// <param name="k">提取数量。</param>
    /// <param name="costModel">成本模型。</param>
    /// <returns>Top-K 提取结果。</returns>
    public static TopKExtractionResult extract_top_k(
        EGraph<AlgebraNode> egraph,
        Id root,
        int k,
        ICostModel costModel)
    {
        var beamCache = build_top_k_cache(egraph, root, k, costModel);

        var nodes = new List<AlgebraNode>(Math.Min(k, beamCache.Count));
        var costs = new List<CostVector>(Math.Min(k, beamCache.Count));

        var rootId = egraph.union_find.find(root);
        if (!beamCache.TryGetValue(rootId.value, out var candidates) || candidates.Count == 0)
        {
            var fallback = new Extractor(egraph, costModel);
            var fallbackNode = fallback.extract(root);
            nodes.Add(fallbackNode);
            var fallbackCost = fallback.cache.try_get_best(rootId.value, out var fc, out _)
                ? fc
                : CostVector.zero;
            costs.Add(fallbackCost);
            return new TopKExtractionResult(nodes, costs);
        }

        var nodeCache = new Dictionary<uint, AlgebraNode>();

        for (var i = 0; i < Math.Min(k, candidates.Count); i++)
        {
            var (cost, candidate) = candidates[i];
            var node = get_node_from_beam(egraph, rootId, beamCache, nodeCache, candidates[i]);
            nodes.Add(node);
            costs.Add(cost);
        }

        return new TopKExtractionResult(nodes, costs);
    }


    /// <summary>
    ///     构建 Beam Top-K 缓存。
    ///     Phase 1：自底向上拓扑排序，计算各等价类的最多 K 个最优候选节点。
    ///     Phase 2：迭代精炼，直到成本收敛或达到最大迭代次数。
    /// </summary>
    private static Dictionary<uint, List<(CostVector, AlgebraNode)>> build_top_k_cache(
        EGraph<AlgebraNode> egraph,
        Id root,
        int beamWidth,
        ICostModel costModel)
    {
        var beamCache = new Dictionary<uint, List<(CostVector, AlgebraNode)>>();
        var rootId = egraph.union_find.find(root);
        var visited = new HashSet<uint>();
        var stack = new Stack<uint>();
        stack.Push(rootId.value);

        while (stack.Count > 0)
        {
            var classId = stack.Pop();
            if (!visited.Add(classId)) continue;

            var eclass = egraph.get_class(new Id(classId));
            if (eclass is null) continue;

            var candidates = build_candidates(egraph, eclass, beamCache, beamWidth, costModel);

            foreach (var candidate in candidates)
            foreach (var child in candidate.Item2.child_ids())
            {
                var childRoot = egraph.union_find.find(child);
                if (!visited.Contains(childRoot.value)) stack.Push(childRoot.value);
            }

            beamCache[classId] = candidates;
        }

        iterative_refinement(egraph, visited, beamCache, beamWidth, costModel);

        return beamCache;
    }

    private static List<(CostVector, AlgebraNode)> build_candidates(
        EGraph<AlgebraNode> egraph,
        EClass<AlgebraNode, object> eclass,
        Dictionary<uint, List<(CostVector, AlgebraNode)>> beamCache,
        int beamWidth,
        ICostModel costModel)
    {
        var candidates = new List<(CostVector, AlgebraNode)>();

        foreach (var node in eclass.nodes)
        {
            var cost = costModel.node_cost(node);
            var valid = true;

            foreach (var child in node.child_ids())
            {
                var childRoot = egraph.union_find.find(child);
                if (!beamCache.TryGetValue(childRoot.value, out var childCandidates) ||
                    childCandidates.Count == 0)
                {
                    valid = false;
                    break;
                }

                cost += childCandidates[0].Item1;
            }

            if (valid) candidates.Add((cost, node));
        }

        candidates.Sort((a, b) => costModel.compare(a.Item1, b.Item1));

        if (candidates.Count > beamWidth) candidates = [.. candidates.Take(beamWidth)];

        return candidates;
    }

    private static void iterative_refinement(
        EGraph<AlgebraNode> egraph,
        HashSet<uint> visited,
        Dictionary<uint, List<(CostVector, AlgebraNode)>> beamCache,
        int beamWidth,
        ICostModel costModel)
    {
        var changed = true;
        const int maxIterations = 10;
        var iteration = 0;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            iteration++;

            foreach (var classId in visited)
            {
                var eclass = egraph.get_class(new Id(classId));
                if (eclass is null) continue;

                var candidates = build_candidates(egraph, eclass, beamCache, beamWidth, costModel);

                var oldBest = beamCache.TryGetValue(classId, out var old) && old.Count > 0
                    ? old[0].Item1
                    : CostVector.max_value;
                var newBest = candidates.Count > 0
                    ? candidates[0].Item1
                    : CostVector.max_value;

                if (costModel.compare(newBest, oldBest) != 0) changed = true;

                beamCache[classId] = candidates;
            }
        }
    }

    private static AlgebraNode get_node_from_beam(
        EGraph<AlgebraNode> egraph,
        Id classId,
        Dictionary<uint, List<(CostVector, AlgebraNode)>> beamCache,
        Dictionary<uint, AlgebraNode> nodeCache,
        (CostVector, AlgebraNode) best)
    {
        if (nodeCache.TryGetValue(classId.value, out var cached)) return cached;

        nodeCache[classId.value] = best.Item2;
        return best.Item2;
    }
}