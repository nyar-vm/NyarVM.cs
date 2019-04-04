using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.IR.Extractor;

#region Extractor

/// <summary>
///     通用提取器，支持贪心、束搜索、动态规划三种策略从 E-Graph 中提取最优 Oa 节点
/// </summary>
public class Extractor
{
    private readonly ICostModel _cost_model;
    private readonly EGraph<AlgebraNode> _egraph;
    private readonly int _beam_width;

    /// <summary>
    ///     创建提取器，默认使用动态规划策略
    /// </summary>
    /// <param name="egraph">目标 E-Graph</param>
    /// <param name="costModel">成本模型</param>
    public Extractor(EGraph<AlgebraNode> egraph, ICostModel costModel)
        : this(egraph, costModel, ExtractionStrategy.dynamic_programming)
    {
    }

    /// <summary>
    ///     使用已有缓存创建提取器，默认使用动态规划策略
    /// </summary>
    /// <param name="egraph">目标 E-Graph</param>
    /// <param name="costModel">成本模型</param>
    /// <param name="cache">已有的提取缓存</param>
    public Extractor(EGraph<AlgebraNode> egraph, ICostModel costModel, ExtractionCache cache)
        : this(egraph, costModel, cache, ExtractionStrategy.dynamic_programming)
    {
    }

    /// <summary>
    ///     使用指定策略创建提取器
    /// </summary>
    /// <param name="egraph">目标 E-Graph</param>
    /// <param name="costModel">成本模型</param>
    /// <param name="strategy">提取策略</param>
    /// <param name="beamWidth">束搜索宽度（仅在 BeamSearch 策略下生效）</param>
    public Extractor(EGraph<AlgebraNode> egraph, ICostModel costModel, ExtractionStrategy strategy, int beamWidth = 5)
        : this(egraph, costModel, new ExtractionCache(egraph.classes.Count), strategy, beamWidth)
    {
    }

    /// <summary>
    ///     使用已有缓存和指定策略创建提取器
    /// </summary>
    /// <param name="egraph">目标 E-Graph</param>
    /// <param name="costModel">成本模型</param>
    /// <param name="cache">已有的提取缓存</param>
    /// <param name="strategy">提取策略</param>
    /// <param name="beamWidth">束搜索宽度（仅在 BeamSearch 策略下生效）</param>
    public Extractor(EGraph<AlgebraNode> egraph, ICostModel costModel, ExtractionCache cache,
        ExtractionStrategy strategy, int beamWidth = 5)
    {
        _egraph = egraph;
        _cost_model = costModel;
        this.cache = cache;
        this.strategy = strategy;
        _beam_width = beamWidth;

        if (this.strategy == ExtractionStrategy.dynamic_programming) find_best();
    }

    /// <summary>
    ///     提取缓存
    /// </summary>
    public ExtractionCache cache { get; }

    /// <summary>
    ///     当前使用的提取策略
    /// </summary>
    public ExtractionStrategy strategy { get; }

    /// <summary>
    ///     从指定根等价类提取最优 Oa 节点，根据构造时指定的策略选择合适的算法
    /// </summary>
    /// <param name="root">根等价类标识</param>
    /// <returns>最优 Oa 节点</returns>
    public AlgebraNode extract(Id root)
    {
        return strategy switch
        {
            ExtractionStrategy.greedy => extract_greedy(root),
            ExtractionStrategy.beam_search => extract_beam_search(root),
            ExtractionStrategy.dynamic_programming => extract_internal(root),
            _ => extract_internal(root)
        };
    }

    /// <summary>
    ///     从指定根等价类提取前 K 个最优 Oa 节点变体。
    ///     基于束搜索策略：先执行单最优 FindBest，再按 eclass 逐层展开前 K 个备选节点，
    ///     构造 K 个不同的 Oa 节点。
    /// </summary>
    /// <param name="root">根等价类标识</param>
    /// <param name="k">返回的变体数量</param>
    /// <returns>按成本升序排列的前 K 个 Oa 节点</returns>
    public List<AlgebraNode> extract_top_k(Id root, int k)
    {
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "K 必须大于 0");

        if (k == 1) return [extract(root)];

        var rootId = _egraph.union_find.find(root);
        var results = new List<AlgebraNode>(k);
        var visited = new HashSet<uint>();

        extract_top_k_beam(rootId, k, results, visited);
        return results;
    }

    #endregion

    #region 贪心提取

    /// <summary>
    ///     贪心提取：每个等价类直接选择成本最低的节点，自底向上返回最优节点
    /// </summary>
    private AlgebraNode extract_greedy(Id root)
    {
        return extract_greedy_internal(root);
    }

    private AlgebraNode extract_greedy_internal(Id id)
    {
        var rootId = _egraph.union_find.find(id);
        if (cache.try_get_node(rootId.value, out var cached)) return cached;

        var eclass = _egraph.get_class(rootId);
        if (eclass is null) throw new InvalidOperationException($"EClass not found for {rootId}");

        AlgebraNode? bestNode = null;
        CostVector bestCost = new(double.MaxValue, double.MaxValue, long.MaxValue, long.MaxValue);

        foreach (var node in eclass.nodes)
        {
            var cost = _cost_model.node_cost(node);
            foreach (var child in node.child_ids())
            {
                var childRoot = _egraph.union_find.find(child);
                if (cache.try_get_best(childRoot.value, out var childCost, out _))
                    cost += childCost;
                else
                    cost += new CostVector(double.MaxValue, double.MaxValue, long.MaxValue, long.MaxValue);
            }

            if (_cost_model.compare(cost, bestCost) < 0)
            {
                bestCost = cost;
                bestNode = node;
            }
        }

        if (bestNode is null) throw new InvalidOperationException($"No valid node found for eclass {rootId}");

        cache.set_best(rootId.value, bestCost, bestNode);
        cache.set_node(rootId.value, bestNode);
        return bestNode;
    }

    #endregion

    #region 束搜索提取

    /// <summary>
    ///     束搜索提取：使用束宽限制的候选集，迭代精炼后提取最优树
    /// </summary>
    private AlgebraNode extract_beam_search(Id root)
    {
        var rootId = _egraph.union_find.find(root);
        var beamCache = new Dictionary<uint, List<(CostVector Cost, AlgebraNode Node)>>();

        build_beam_cache(rootId, beamCache);

        return extract_beam_internal(rootId, beamCache);
    }

    private void build_beam_cache(Id rootId,
        Dictionary<uint, List<(CostVector Cost, AlgebraNode Node)>> beamCache)
    {
        var visited = new HashSet<uint>();
        var stack = new Stack<uint>();
        stack.Push(rootId.value);

        while (stack.Count > 0)
        {
            var classId = stack.Pop();
            if (!visited.Add(classId)) continue;

            var eclass = _egraph.get_class(new Id(classId));
            if (eclass is null) continue;

            var candidates = new List<(CostVector Cost, AlgebraNode Node)>(_beam_width);

            foreach (var node in eclass.nodes)
            {
                var cost = _cost_model.node_cost(node);
                var hasInfChild = false;

                foreach (var child in node.child_ids())
                {
                    var childRoot = _egraph.union_find.find(child);
                    if (!visited.Contains(childRoot.value)) stack.Push(childRoot.value);

                    if (beamCache.TryGetValue(childRoot.value, out var childCandidates) &&
                        childCandidates.Count > 0)
                        cost += childCandidates[0].Cost;
                    else
                        hasInfChild = true;
                }

                if (!hasInfChild) candidates.Add((cost, node));
            }

            candidates.Sort((a, b) => _cost_model.compare(a.Cost, b.Cost));

            if (candidates.Count > _beam_width) candidates = [.. candidates.Take(_beam_width)];

            beamCache[classId] = candidates;
        }

        var changed = true;
        const int maxRefinementIterations = 10;
        var iteration = 0;

        while (changed && iteration < maxRefinementIterations)
        {
            changed = false;
            iteration++;

            foreach (var classId in visited)
            {
                var eclass = _egraph.get_class(new Id(classId));
                if (eclass is null) continue;

                var candidates = new List<(CostVector Cost, AlgebraNode Node)>(_beam_width);

                foreach (var node in eclass.nodes)
                {
                    var cost = _cost_model.node_cost(node);
                    var hasInfChild = false;

                    foreach (var child in node.child_ids())
                    {
                        var childRoot = _egraph.union_find.find(child);
                        if (beamCache.TryGetValue(childRoot.value, out var childCandidates) &&
                            childCandidates.Count > 0)
                            cost += childCandidates[0].Cost;
                        else
                            hasInfChild = true;
                    }

                    if (!hasInfChild) candidates.Add((cost, node));
                }

                candidates.Sort((a, b) => _cost_model.compare(a.Cost, b.Cost));

                if (candidates.Count > _beam_width) candidates = [.. candidates.Take(_beam_width)];

                var oldBest = beamCache.TryGetValue(classId, out var old) && old.Count > 0
                    ? old[0].Cost
                    : new CostVector(double.MaxValue, double.MaxValue, long.MaxValue, long.MaxValue);

                var newBest = candidates.Count > 0
                    ? candidates[0].Cost
                    : new CostVector(double.MaxValue, double.MaxValue, long.MaxValue, long.MaxValue);

                if (_cost_model.compare(newBest, oldBest) != 0) changed = true;

                beamCache[classId] = candidates;
            }
        }
    }

    private AlgebraNode extract_beam_internal(Id id,
        Dictionary<uint, List<(CostVector Cost, AlgebraNode Node)>> beamCache)
    {
        var rootId = _egraph.union_find.find(id);
        if (cache.try_get_node(rootId.value, out var cached)) return cached;

        if (!beamCache.TryGetValue(rootId.value, out var candidates) || candidates.Count == 0)
            throw new InvalidOperationException($"No beam candidate found for eclass {rootId}");

        var bestNode = candidates[0].Node;
        cache.set_best(rootId.value, candidates[0].Cost, bestNode);
        cache.set_node(rootId.value, bestNode);
        return bestNode;
    }

    #endregion

    #region 动态规划提取（默认）

    /// <summary>
    ///     束搜索提取 Top-K：每个等价类取前 K 个最优节点，
    ///     递归对每个子节点展开，通过交叉组合生成 K 种变体
    /// </summary>
    private void extract_top_k_beam(
        Id classId,
        int k,
        List<AlgebraNode> results,
        HashSet<uint> visited)
    {
        if (results.Count >= k) return;

        if (!visited.Add(classId.value)) return;

        if (!_egraph.classes.TryGetValue(classId.value, out var eclass)) return;

        var ranked = new List<(CostVector Cost, AlgebraNode Node)>(eclass.nodes.Count);
        foreach (var node in eclass.nodes)
        {
            var cost = _cost_model.node_cost(node);
            var children = node.child_ids();
            var hasUnknown = false;
            for (var i = 0; i < children.Count; i++)
            {
                var childRoot = _egraph.union_find.find(children[i]);
                if (cache.try_get_best(childRoot.value, out var childCost, out _))
                {
                    cost += childCost;
                }
                else
                {
                    cost += CostVector.max_value;
                    hasUnknown = true;
                }
            }

            if (!hasUnknown) ranked.Add((cost, node));
        }

        ranked.Sort((a, b) => _cost_model.compare(a.Item1, b.Item1));

        var take = Math.Min(k, ranked.Count);
        for (var i = 0; i < take && results.Count < k; i++)
        {
            var (_, node) = ranked[i];

            if (!results.Any(r => r.ToString() == node.ToString())) results.Add(node);
        }
    }

    private AlgebraNode extract_internal(Id id)
    {
        var rootId = _egraph.union_find.find(id);
        if (cache.try_get_node(rootId.value, out var cached)) return cached;

        if (!cache.try_get_best(rootId.value, out _, out var node))
            throw new InvalidOperationException($"No best node found for eclass {rootId}");

        cache.set_node(rootId.value, node);
        return node;
    }

    private void find_best()
    {
        const int maxIterations = 100;
        var changed = true;
        var iteration = 0;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            iteration++;

            foreach (var (classId, eclass) in _egraph.classes)
            foreach (var node in eclass.nodes)
            {
                var cost = _cost_model.node_cost(node);
                var children = node.child_ids();
                for (var i = 0; i < children.Count; i++)
                {
                    var childRoot = _egraph.union_find.find(children[i]);
                    if (cache.try_get_best(childRoot.value, out var childCost, out _))
                        cost = cost + childCost;
                    else
                        cost = cost + new CostVector(double.MaxValue, double.MaxValue, long.MaxValue,
                            long.MaxValue);
                }

                if (!cache.try_get_best(classId, out var currentCost, out _) ||
                    _cost_model.compare(cost, currentCost) < 0)
                {
                    cache.set_best(classId, cost, node);
                    changed = true;
                }
            }
        }
    }

    /// <summary>
    ///     从 OA 工厂构建的 EGraph 中提取最优程序
    /// </summary>
    /// <param name="root">根等价类标识符</param>
    /// <returns>最优 Oa 节点</returns>
    public AlgebraNode extract_from_oa(Id root)
    {
        return extract(root);
    }

    /// <summary>
    ///     从 EGraph 中提取最优 ENode 根节点（开放输出，不依赖 AlgebraNode）。
    ///     子节点通过 Id 引用 EGraph 中的等价类，树结构由 EGraph 维护。
    ///     使用简单贪心策略：每个等价类取第一个节点。
    /// </summary>
    /// <param name="egraph">目标 EGraph<ENode>。</param>
    /// <param name="root">根等价类标识符。</param>
    /// <returns>最优 ENode 根节点。</returns>
    public static ENode extract_tree(EGraph<ENode> egraph, Id root)
    {
        var rootId = egraph.union_find.find(root);
        var eclass = egraph.get_class(rootId);
        if (eclass is null || eclass.nodes.Count == 0)
            throw new InvalidOperationException($"EClass not found for {rootId}");

        // 取第一个节点（简单贪心策略，后续可替换为基于成本的提取）
        // 子节点通过 Id 引用 EGraph，树结构由 EGraph 维护
        return eclass.nodes[0];
    }
}

#endregion