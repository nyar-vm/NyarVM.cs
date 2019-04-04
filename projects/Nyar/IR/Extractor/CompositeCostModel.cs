using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.IR.Extractor;

/// <summary>
///     统一组合成本模型，支持从方言钩子、ICostModelHook 列表、后端模型三种来源构建，
///     通过权重向量对多维度成本（延迟/功耗/内存/面积）进行加权综合评估
/// </summary>
public sealed class CompositeCostModel : ICostModel
{
    private readonly List<(string Backend, ICostModel Model, CostWeights Weights)> _backends;
    private readonly List<ICostModelHook> _hooks;

    /// <summary>
    ///     创建组合成本模型
    /// </summary>
    /// <param name="globalWeights">全局维度权重</param>
    public CompositeCostModel(CostWeights? globalWeights = null)
    {
        _backends = [];
        _hooks = [];
        weights = globalWeights ?? CostWeights.@default;
        goal = OptimizationGoal.balanced;
    }

    /// <summary>
    ///     从成本钩子列表创建组合成本模型
    /// </summary>
    /// <param name="hooks">成本钩子列表</param>
    /// <param name="goal">优化目标</param>
    public CompositeCostModel(IEnumerable<ICostModelHook> hooks,
        OptimizationGoal goal = OptimizationGoal.minimize_latency)
    {
        _hooks = [.. hooks];
        _backends = [];
        weights = goal.to_cost_weights();
        this.goal = goal;
    }

    /// <summary>
    ///     从成本钩子列表和自定义权重创建组合成本模型
    /// </summary>
    /// <param name="hooks">成本钩子列表</param>
    /// <param name="weights">成本权重</param>
    public CompositeCostModel(IEnumerable<ICostModelHook> hooks, CostWeights weights)
    {
        _hooks = [.. hooks];
        _backends = [];
        this.weights = weights;
        goal = OptimizationGoal.balanced;
    }

    /// <summary>
    ///     从方言列表创建组合成本模型
    /// </summary>
    /// <param name="dialects">方言列表</param>
    /// <param name="goal">优化目标</param>
    public CompositeCostModel(IEnumerable<IDialect> dialects, OptimizationGoal goal = OptimizationGoal.minimize_latency)
    {
        _hooks = [.. dialects.SelectMany(d => d.cost_hooks)];
        _backends = [];
        weights = goal.to_cost_weights();
        this.goal = goal;
    }

    /// <summary>
    ///     从方言列表和自定义权重创建组合成本模型
    /// </summary>
    /// <param name="dialects">方言列表</param>
    /// <param name="weights">成本权重</param>
    public CompositeCostModel(IEnumerable<IDialect> dialects, CostWeights weights)
    {
        _hooks = [.. dialects.SelectMany(d => d.cost_hooks)];
        _backends = [];
        this.weights = weights;
        goal = OptimizationGoal.balanced;
    }

    /// <summary>
    ///     优化目标
    /// </summary>
    public OptimizationGoal goal { get; }

    /// <summary>
    ///     成本权重
    /// </summary>
    public CostWeights weights { get; }

    /// <summary>
    ///     获取已注册的后端列表
    /// </summary>
    public IReadOnlyList<(string Backend, ICostModel Model, CostWeights Weights)> backends => _backends.AsReadOnly();

    /// <summary>
    ///     获取已注册的成本钩子列表
    /// </summary>
    public IReadOnlyList<ICostModelHook> hooks => _hooks.AsReadOnly();

    /// <inheritdoc />
    public CostVector node_cost(AlgebraNode node)
    {
        if (_hooks.Count > 0)
        {
            foreach (var hook in _hooks)
                if (hook.can_handle(node))
                    return hook.estimate(node);

            return CostVector.from_latency(1);
        }

        if (_backends.Count == 0) return CostVector.zero;

        var total = CostVector.zero;
        for (var i = 0; i < _backends.Count; i++) total += _backends[i].Model.node_cost(node);

        var count = _backends.Count;
        return new CostVector(
            total.latency / count,
            total.power / count,
            total.memory / count,
            total.area / count);
    }

    /// <inheritdoc />
    public int compare(CostVector a, CostVector b)
    {
        return weights.compare(a, b);
    }

    /// <summary>
    ///     注册后端的成本模型
    /// </summary>
    /// <param name="backendName">后端名称</param>
    /// <param name="costModel">成本模型</param>
    /// <param name="weights">该后端的维度权重</param>
    public void register_backend(string backendName, ICostModel costModel, CostWeights? weights = null)
    {
        _backends.Add((backendName, costModel, weights ?? this.weights));
    }

    /// <summary>
    ///     移除后端注册
    /// </summary>
    /// <param name="backendName">后端名称</param>
    /// <returns>是否成功移除</returns>
    public bool unregister_backend(string backendName)
    {
        return _backends.RemoveAll(b => b.Backend == backendName) > 0;
    }
}