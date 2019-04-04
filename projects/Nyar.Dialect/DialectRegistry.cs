using System.Collections.Immutable;
using System.Text;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;
using Nyar.PartialEvaluate;
using Nyar.Types;

namespace Nyar.Dialect;

/// <summary>
///     方言注册表，管理所有已注册的方言实例
/// </summary>
/// <remarks>
///     ⛔ 注册方法依赖 IDialect / AlgebraNode 遗留模型，已冻结。
///     新的方言应通过 <see cref="IDialectRuntime" /> + <see cref="IDialectDefinition" /> 注册。
///     详见 .trae/specs/oa-full-pipeline/spec.md。
/// </remarks>
[Obsolete("DialectRegistry 依赖 IDialect / AlgebraNode 遗留模型。请使用 IDialectRuntime + IDialectDefinition 开放模型")]
public sealed class DialectRegistry
{
    private readonly Dictionary<string, List<IDialect>> _dependents;
    private readonly Dictionary<int, IDialect> _dialects_by_id;
    private readonly Dictionary<string, IDialect> _dialects_by_name;
    private readonly List<IDialect> _registration_order;
    private ImmutableDictionary<int, IDialect>? _cached_lookup;

    public DialectRegistry()
    {
        _dialects_by_id = new Dictionary<int, IDialect>();
        _dialects_by_name = new Dictionary<string, IDialect>();
        _dependents = new Dictionary<string, List<IDialect>>();
        _registration_order = [];
    }

    /// <summary>
    ///     已注册的方言列表（按注册顺序）
    /// </summary>
    public IReadOnlyList<IDialect> dialects => _registration_order;

    /// <summary>
    ///     注册表是否已冻结
    /// </summary>
    public bool is_frozen { get; private set; }

    /// <summary>
    ///     从方言名称计算确定性哈希 ID（FNV-1a 32-bit）
    /// </summary>
    public static int compute_id(string name)
    {
        const uint fnvOffsetBasis = 2166136261u;
        const uint fnvPrime = 16777619u;

        var hash = fnvOffsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(name))
        {
            hash ^= b;
            hash *= fnvPrime;
        }

        return (int)hash;
    }

    /// <summary>
    ///     注册方言到注册表
    /// </summary>
    public DialectRegistry register(IDialect dialect)
    {
        if (is_frozen) throw new InvalidOperationException("方言注册表已冻结，无法注册新方言");

        var id = compute_id(dialect.name);

        if (_dialects_by_id.TryGetValue(id, out var value))
            throw new ArgumentException($"方言名称 '{dialect.name}' 的哈希 ID 0x{id:X8} 与已注册方言 '{value.name}' 冲突");

        if (_dialects_by_name.ContainsKey(dialect.name)) throw new ArgumentException($"方言名称 '{dialect.name}' 已被注册");

        _dialects_by_id[id] = dialect;
        _dialects_by_name[dialect.name] = dialect;
        _registration_order.Add(dialect);

        foreach (var target in dialect.lowering_targets)
        {
            if (!_dependents.ContainsKey(target.name)) _dependents[target.name] = [];

            _dependents[target.name].Add(dialect);
        }

        _cached_lookup = null;
        return this;
    }

    /// <summary>
    ///     批量注册方言
    /// </summary>
    public DialectRegistry register_range(IEnumerable<IDialect> dialects)
    {
        foreach (var dialect in dialects) register(dialect);

        return this;
    }

    /// <summary>
    ///     根据名称的哈希 ID 查找方言
    /// </summary>
    public IDialect? get_by_id(int id)
    {
        return _dialects_by_id.GetValueOrDefault(id);
    }

    /// <summary>
    ///     根据名称查找方言
    /// </summary>
    public IDialect? get_by_name(string name)
    {
        return _dialects_by_name.GetValueOrDefault(name);
    }

    /// <summary>
    ///     尝试根据哈希 ID 查找方言
    /// </summary>
    public bool try_get_by_id(int id, out IDialect? dialect)
    {
        return _dialects_by_id.TryGetValue(id, out dialect);
    }

    /// <summary>
    ///     尝试根据名称查找方言
    /// </summary>
    public bool try_get_by_name(string name, out IDialect? dialect)
    {
        return _dialects_by_name.TryGetValue(name, out dialect);
    }

    /// <summary>
    ///     获取依赖于指定方言的所有方言
    /// </summary>
    public IReadOnlyList<IDialect> get_dependents(string dialectName)
    {
        return _dependents.TryGetValue(dialectName, out var deps)
            ? deps
            : Array.Empty<IDialect>();
    }

    /// <summary>
    ///     将所有方言的等价规则注册到 EGraph，并执行饱和优化
    /// </summary>
    public void register_all_rules(EGraph<AlgebraNode> egraph, int maxIterations = 10)
    {
        var allRules = new List<IRewriteRule<AlgebraNode>>();
        foreach (var dialect in _registration_order) allRules.AddRange(dialect.rules);

        var engine = new SaturationEngine<AlgebraNode>(allRules, maxIterations);
        engine.run(egraph);
    }

    /// <summary>
    ///     获取所有方言的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<AlgebraNode>> get_all_rules()
    {
        var rules = new List<IRewriteRule<AlgebraNode>>();
        foreach (var dialect in _registration_order) rules.AddRange(dialect.rules);

        return rules;
    }

    /// <summary>
    ///     获取所有方言的 PE 工厂
    /// </summary>
    public IReadOnlyList<IPartialEvaluateFactory> get_all_pe_factories()
    {
        var factories = new List<IPartialEvaluateFactory>();
        foreach (var dialect in _registration_order) factories.AddRange(dialect.pe_factories);

        return factories;
    }

    /// <summary>
    ///     获取所有方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> get_all_cost_hooks()
    {
        var hooks = new List<ICostModelHook>();
        foreach (var dialect in _registration_order) hooks.AddRange(dialect.cost_hooks);

        return hooks;
    }

    /// <summary>
    ///     构建组合成本模型
    /// </summary>
    public CompositeCostModel build_cost_model()
    {
        var hooks = get_all_cost_hooks();
        return new CompositeCostModel(hooks, CostWeights.@default);
    }

    /// <summary>
    ///     获取方言的拓扑排序（依赖在前）
    /// </summary>
    public IReadOnlyList<IDialect> get_topological_order()
    {
        var visited = new HashSet<string>();
        var result = new List<IDialect>();
        var visiting = new HashSet<string>();

        foreach (var dialect in _registration_order) visit_dialect(dialect, visited, visiting, result);

        return result;
    }

    /// <summary>
    ///     验证方言依赖关系是否合法
    /// </summary>
    public bool validate_dependencies(out List<string> errors)
    {
        errors = [];

        foreach (var dialect in _registration_order)
        foreach (var target in dialect.lowering_targets)
            if (!_dialects_by_name.ContainsKey(target.name))
                errors.Add($"方言 '{dialect.name}' 依赖未注册的降级目标 '{target.name}'");

        if (has_circular_dependency()) errors.Add("检测到循环依赖");

        return errors.Count == 0;
    }

    /// <summary>
    ///     冻结注册表，禁止后续注册
    /// </summary>
    public DialectRegistry freeze()
    {
        is_frozen = true;
        _cached_lookup = _dialects_by_id.ToImmutableDictionary();
        return this;
    }

    private void visit_dialect(IDialect dialect, HashSet<string> visited, HashSet<string> visiting,
        List<IDialect> result)
    {
        if (visited.Contains(dialect.name)) return;

        if (!visiting.Add(dialect.name)) return;

        foreach (var target in dialect.lowering_targets)
            if (_dialects_by_name.TryGetValue(target.name, out var targetDialect))
                visit_dialect(targetDialect, visited, visiting, result);

        visiting.Remove(dialect.name);
        visited.Add(dialect.name);
        result.Add(dialect);
    }

    private bool has_circular_dependency()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var dialect in _registration_order)
            if (has_circular_dependency_dfs(dialect.name, visited, recursionStack))
                return true;

        return false;
    }

    private bool has_circular_dependency_dfs(string name, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(name)) return true;

        if (!visited.Add(name)) return false;

        recursionStack.Add(name);

        if (_dialects_by_name.TryGetValue(name, out var dialect))
            foreach (var target in dialect.lowering_targets)
                if (has_circular_dependency_dfs(target.name, visited, recursionStack))
                    return true;

        recursionStack.Remove(name);
        return false;
    }
}