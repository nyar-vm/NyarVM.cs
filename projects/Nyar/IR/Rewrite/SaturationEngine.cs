using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;

namespace Nyar.IR.Rewrite;

/// <summary>
///     饱和优化引擎，重复应用重写规则直到不动点。
///     支持 Worklist 驱动、进度回调、错误回调、方言工厂方法和有限步数饱和。
/// </summary>
/// <typeparam name="T">语言节点类型</typeparam>
public class SaturationEngine<T> where T : ILanguage<T>
{
    #region 构造函数

    /// <summary>
    ///     创建饱和优化引擎
    /// </summary>
    /// <param name="rules">重写规则列表。</param>
    /// <param name="maxIterations">最大迭代次数。</param>
    /// <param name="maxNodesPerClass">每个等价类最大节点数（防止爆炸），0 表示不限制。</param>
    /// <param name="useWorklist">是否启用 Worklist 驱动模式。</param>
    public SaturationEngine(
        IEnumerable<IRewriteRule<T>> rules,
        int maxIterations = 10,
        int maxNodesPerClass = 1000,
        bool useWorklist = false)
    {
        _rules = [.. rules];
        _max_iterations = maxIterations;
        _max_nodes_per_class = maxNodesPerClass;
        _use_worklist = useWorklist;
    }

    #endregion

    #region 简单迭代模式

    /// <summary>
    ///     简单迭代饱和：每轮遍历所有规则，直到不动点或最大迭代次数
    /// </summary>
    private SaturationResult run_simple(EGraph<T> egraph)
    {
        var totalUnions = 0;
        var iteration = 0;
        var changed = true;

        while (changed && iteration < _max_iterations)
        {
            changed = false;
            iteration++;

            foreach (var rule in _rules)
                try
                {
                    if (rule.apply(egraph))
                    {
                        changed = true;
                        totalUnions++;
                    }
                }
                catch (Exception ex)
                {
                    on_rule_error?.Invoke(rule.name, 0, ex);
                }

            egraph.rebuild();

            on_progress?.Invoke(iteration, egraph.classes.Count, !changed);
        }

        return new SaturationResult(iteration, totalUnions, !changed);
    }

    #endregion

    #region Worklist 驱动模式

    /// <summary>
    ///     Worklist 驱动饱和：使用队列追踪受影响的等价类，仅重新处理变更的类
    /// </summary>
    private SaturationResult run_worklist(EGraph<T> egraph)
    {
        var totalUnions = 0;
        var iteration = 0;

        var worklist = new Queue<uint>();
        foreach (var classId in egraph.classes.Keys) worklist.Enqueue(classId);

        while (iteration < _max_iterations && worklist.Count > 0)
        {
            iteration++;
            var saturated = true;
            var currentBatch = new List<uint>();

            while (worklist.Count > 0) currentBatch.Add(worklist.Dequeue());

            var affectedClasses = new HashSet<uint>();

            foreach (var classId in currentBatch)
            {
                if (!egraph.classes.TryGetValue(classId, out var eclass)) continue;

                if (_max_nodes_per_class > 0 && eclass.nodes.Count >= _max_nodes_per_class) continue;

                foreach (var rule in _rules)
                    try
                    {
                        if (rule.apply(egraph))
                        {
                            saturated = false;
                            totalUnions++;
                            affectedClasses.Add(classId);

                            foreach (var siblingId in egraph.classes.Keys) affectedClasses.Add(siblingId);
                        }
                    }
                    catch (Exception ex)
                    {
                        on_rule_error?.Invoke(rule.name, classId, ex);
                    }
            }

            egraph.rebuild();

            foreach (var affectedId in affectedClasses) worklist.Enqueue(affectedId);

            on_progress?.Invoke(iteration, affectedClasses.Count, saturated);

            if (saturated) break;
        }

        var isSaturated = worklist.Count == 0;
        return new SaturationResult(iteration, totalUnions, isSaturated);
    }

    #endregion

    #region 字段

    private readonly List<IRewriteRule<T>> _rules;
    private readonly int _max_iterations;
    private readonly int _max_nodes_per_class;
    private readonly bool _use_worklist;

    #endregion

    #region 属性

    /// <summary>
    ///     获取已注册的规则列表
    /// </summary>
    public IReadOnlyList<IRewriteRule<T>> rules => _rules.AsReadOnly();

    /// <summary>
    ///     进度回调：iteration, affectedClassCount, saturated
    /// </summary>
    public Action<int, int, bool>? on_progress { get; set; }

    /// <summary>
    ///     规则应用错误回调：ruleName, classId, exception
    /// </summary>
    public Action<string, uint, Exception>? on_rule_error { get; set; }

    #endregion

    #region 规则管理

    /// <summary>
    ///     注册新的重写规则
    /// </summary>
    /// <param name="rule">重写规则。</param>
    public void add_rule(IRewriteRule<T> rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    ///     添加一组 OA 风格的重写规则
    /// </summary>
    /// <param name="rules">OA 重写规则列表。</param>
    public void add_oa_rules(IEnumerable<IRewriteRule<T>> rules)
    {
        foreach (var rule in rules) add_rule(rule);
    }

    #endregion

    #region 饱和优化核心

    /// <summary>
    ///     对 E-Graph 运行饱和优化
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <returns>优化统计信息。</returns>
    public SaturationResult run(EGraph<T> egraph)
    {
        if (_use_worklist) return run_worklist(egraph);

        return run_simple(egraph);
    }

    /// <summary>
    ///     对 E-Graph 执行饱和优化（返回是否饱和）
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <returns>是否达到不动点（饱和）。</returns>
    public bool saturate(EGraph<T> egraph)
    {
        var result = run(egraph);
        return result.is_saturated;
    }

    /// <summary>
    ///     对 E-Graph 执行有限步数的饱和优化
    /// </summary>
    /// <param name="egraph">E-Graph 实例。</param>
    /// <param name="steps">步数限制。</param>
    public void saturate_for_steps(EGraph<T> egraph, int steps)
    {
        var engine = new SaturationEngine<T>(_rules, steps, _max_nodes_per_class, _use_worklist);
        engine.on_progress = on_progress;
        engine.on_rule_error = on_rule_error;
        engine.run(egraph);
    }

    #endregion

    #region 工厂方法

    /// <summary>
    ///     从方言列表创建饱和引擎
    /// </summary>
    /// <param name="dialects">方言列表。</param>
    /// <param name="maxIterations">最大迭代次数。</param>
    /// <param name="useWorklist">是否启用 Worklist 模式。</param>
    /// <returns>饱和引擎实例。</returns>
    public static SaturationEngine<T> from_dialects(
        IEnumerable<IDialect> dialects,
        int maxIterations = 10,
        bool useWorklist = false)
    {
        if (typeof(T) != typeof(AlgebraNode))
            throw new InvalidOperationException(
                $"FromDialects 仅适用于 Oa 类型，当前类型为 {typeof(T).Name}");

        var rules = new List<IRewriteRule<T>>();
        foreach (var dialect in dialects)
        foreach (var rule in dialect.rules)
            if (rule is IRewriteRule<T> typedRule)
                rules.Add(typedRule);

        return new SaturationEngine<T>(rules, maxIterations, useWorklist: useWorklist);
    }

    /// <summary>
    ///     从方言列表创建饱和引擎（带 PE 降级）
    /// </summary>
    /// <param name="dialects">方言列表。</param>
    /// <param name="usePeFactories">是否使用 PE 工厂而非普通规则。</param>
    /// <param name="maxIterations">最大迭代次数。</param>
    /// <param name="useWorklist">是否启用 Worklist 模式。</param>
    /// <returns>饱和引擎实例。</returns>
    public static SaturationEngine<T> from_dialect_rules(
        IEnumerable<IDialect> dialects,
        bool usePeFactories = false,
        int maxIterations = 10,
        bool useWorklist = false)
    {
        if (typeof(T) != typeof(AlgebraNode))
            throw new InvalidOperationException(
                $"FromDialectRules 仅适用于 Oa 类型，当前类型为 {typeof(T).Name}");

        var rules = new List<IRewriteRule<T>>();
        foreach (var dialect in dialects)
        {
            var source = usePeFactories ? [] : dialect.rules;
            foreach (var rule in source)
                if (rule is IRewriteRule<T> typedRule)
                    rules.Add(typedRule);
        }

        return new SaturationEngine<T>(rules, maxIterations, useWorklist: useWorklist);
    }

    #endregion
}