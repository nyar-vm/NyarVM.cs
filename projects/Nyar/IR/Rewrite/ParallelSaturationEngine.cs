using System.Collections.Concurrent;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.IR.Rewrite;

/// <summary>
///     并行饱和优化引擎，使用多线程加速规则匹配-应用循环。
///     采用快照并行策略：先拍摄 EGraph 等价类快照，各规则并行在快照上匹配，
///     收集 Union 对后在主线程统一应用，最后执行一次 Rebuild。
/// </summary>
/// <typeparam name="T">语言节点类型</typeparam>
public class ParallelSaturationEngine<T> where T : ILanguage<T>
{
    private readonly int _max_iterations;
    private readonly ParallelOptions _parallel_options;
    private readonly List<IRewriteRule<T>> _rules;

    /// <summary>
    ///     创建并行饱和优化引擎
    /// </summary>
    /// <param name="rules">重写规则列表。</param>
    /// <param name="maxIterations">最大迭代次数。</param>
    /// <param name="maxDegreeOfParallelism">最大并行度，-1 表示不限制。</param>
    public ParallelSaturationEngine(
        IEnumerable<IRewriteRule<T>> rules,
        int maxIterations = 10,
        int maxDegreeOfParallelism = -1)
    {
        _rules = [.. rules];
        _max_iterations = maxIterations;
        _parallel_options = new ParallelOptions();
        if (maxDegreeOfParallelism > 0) _parallel_options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    ///     获取已注册的规则列表
    /// </summary>
    public IReadOnlyList<IRewriteRule<T>> rules => _rules.AsReadOnly();

    /// <summary>
    ///     注册新的重写规则
    /// </summary>
    /// <param name="rule">重写规则。</param>
    public void add_rule(IRewriteRule<T> rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    ///     对 E-Graph 运行并行饱和优化
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <returns>优化统计信息。</returns>
    public SaturationResult run(EGraph<T> egraph)
    {
        var totalUnions = 0;
        var iteration = 0;
        var changed = true;

        while (changed && iteration < _max_iterations)
        {
            changed = false;
            iteration++;

            var snapshot = take_snapshot(egraph);
            var unionBag = new ConcurrentBag<(Id, Id)>();

            Parallel.ForEach(_rules, _parallel_options, rule =>
            {
                var localChanges = MatchOnSnapshot(egraph, rule, snapshot);
                if (localChanges is not null)
                    foreach (var pair in localChanges)
                        unionBag.Add(pair);
            });

            foreach (var (id1, id2) in unionBag)
            {
                egraph.union(id1, id2);
                changed = true;
                totalUnions++;
            }

            if (changed) egraph.rebuild();
        }

        return new SaturationResult(iteration, totalUnions, !changed);
    }

    /// <summary>
    ///     拍摄 EGraph 等价类快照，生成只读的 (类ID, 节点列表) 数组
    /// </summary>
    private static (Id ClassId, T[] Nodes)[] take_snapshot(EGraph<T> egraph)
    {
        var snapshot = new (Id, T[])[egraph.classes.Count];
        var idx = 0;
        foreach (var (classId, eclass) in egraph.classes) snapshot[idx++] = (new Id(classId), [.. eclass.nodes]);

        return snapshot;
    }

    /// <summary>
    ///     在快照上执行单条规则的匹配，返回发现的等价类合并对
    /// </summary>
    private static List<(Id, Id)>? MatchOnSnapshot(
        EGraph<T> egraph,
        IRewriteRule<T> rule,
        (Id ClassId, T[] Nodes)[] snapshot)
    {
        List<(Id, Id)>? results = null;

        foreach (var (classId, nodes) in snapshot)
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (!matches_node(rule, node)) continue;

                var newId = rewrite_node(rule, egraph, node);
                if (newId is null) continue;

                var canonicalNew = egraph.union_find.find(newId.Value);
                var existingId = new Id(canonicalNew.value);

                if (existingId.value != classId.value)
                {
                    results ??= [];
                    results.Add((classId, existingId));
                }
            }

        return results;
    }

    /// <summary>
    ///     判断节点是否匹配规则的匹配器
    /// </summary>
    private static bool matches_node(IRewriteRule<T> rule, T node)
    {
        if (rule is PatternRule<T> patternRule) return patternRule.matches(node);

        return true;
    }

    /// <summary>
    ///     对匹配的节点应用规则的重写器
    /// </summary>
    private static Id? rewrite_node(IRewriteRule<T> rule, EGraph<T> egraph, T node)
    {
        if (rule is PatternRule<T> patternRule) return patternRule.rewrite(egraph, node);

        return null;
    }
}