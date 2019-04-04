using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;
using Nyar.PartialEvaluate;

namespace Nyar.Dialect;

/// <summary>
///     组合方言，将多个方言的规则和 PE 工厂合并到统一的集合中
/// </summary>
public sealed class CompositeDialect
{
    private readonly List<IPartialEvaluateFactory> _combined_pe_factories;
    private readonly List<IRewriteRule<AlgebraNode>> _combined_rules;
    private readonly DialectRegistry _registry;
    private bool _initialized;

    /// <summary>
    ///     创建组合方言
    /// </summary>
    /// <param name="name">组合方言名称。</param>
    /// <param name="registry">方言注册表。</param>
    /// <param name="sourceDialects">源方言列表。</param>
    public CompositeDialect(string name, DialectRegistry registry, IEnumerable<IDialect> sourceDialects)
    {
        this.name = name;
        _registry = registry;
        source_dialects = [.. sourceDialects];
        _combined_rules = [];
        _combined_pe_factories = [];
        cost_model = registry.build_cost_model();
        _initialized = false;
    }

    /// <summary>
    ///     组合方言名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     源方言列表
    /// </summary>
    public IReadOnlyList<IDialect> source_dialects { get; }

    /// <summary>
    ///     合并后的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<AlgebraNode>> combined_rules => _combined_rules;

    /// <summary>
    ///     合并后的 PE 工厂
    /// </summary>
    public IReadOnlyList<IPartialEvaluateFactory> combined_pe_factories => _combined_pe_factories;

    /// <summary>
    ///     组合成本模型
    /// </summary>
    public ICostModel cost_model { get; }

    /// <summary>
    ///     初始化组合方言，递归收集所有源方言的规则和 PE 工厂
    /// </summary>
    /// <returns>当前实例。</returns>
    public CompositeDialect initialize()
    {
        if (_initialized) return this;

        var visited = new HashSet<string>();

        foreach (var dialect in source_dialects) collect_rules_recursive(dialect, visited);

        _initialized = true;
        return this;
    }

    /// <summary>
    ///     使用统一饱和引擎对 E-Graph 执行饱和优化
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <param name="maxIterations">最大迭代次数。</param>
    /// <returns>饱和结果。</returns>
    public SaturationResult saturate(EGraph<AlgebraNode> egraph, int maxIterations = 10)
    {
        if (!_initialized) initialize();

        var engine = new SaturationEngine<AlgebraNode>(_combined_rules, maxIterations);
        return engine.run(egraph);
    }

    /// <summary>
    ///     获取所有 PE 工厂
    /// </summary>
    /// <returns>PE 工厂列表。</returns>
    public IReadOnlyList<IPartialEvaluateFactory> get_pe_factories()
    {
        if (!_initialized) initialize();

        return _combined_pe_factories;
    }

    /// <summary>
    ///     对所有 PE 工厂应用降级
    /// </summary>
    /// <param name="egraph">目标 EGraph。</param>
    public void apply_pe_factories(EGraph<AlgebraNode> egraph)
    {
        if (!_initialized) initialize();

        foreach (var factory in _combined_pe_factories) factory.apply(egraph);
    }

    private void collect_rules_recursive(IDialect dialect, HashSet<string> visited)
    {
        if (!visited.Add(dialect.name)) return;

        _combined_rules.AddRange(dialect.rules);

        foreach (var target in dialect.lowering_targets) collect_rules_recursive(target, visited);

        _combined_pe_factories.AddRange(dialect.pe_factories);
    }
}