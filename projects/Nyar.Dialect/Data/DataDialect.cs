using Nyar.Dialect.Data.Cost;
using Nyar.Dialect.Standard;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;

namespace Nyar.Dialect.Data;

/// <summary>
///     Data 方言面向数据库查询与数据处理领域
/// </summary>
public sealed class DataDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "data";

    /// <summary>
    ///     Data 方言的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<Oa>> rules => new List<IRewriteRule<Oa>>
    {
        new JoinCommutativeRule(),
        new PredicatePushdownRule(),
        new ProjectMergeRule(),
        new FilterMergeRule(),
        new TrivialFilterRule(),
        new LimitZeroRule(),
        // TODO: Oa.Symbol 类型已移除，此规则待修复
        // new HavingPushdownRule(),
        new GroupByMergeRule(),
        new WindowPruneRule(),
        new EnhancedPredicatePushdownRule(),
        new ProjectPruningRule(),
        new JoinReorderRule()
    };

    /// <summary>
    ///     Data 方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> cost_hooks => new List<ICostModelHook>
    {
        new DataCostHook()
    };

    /// <summary>
    ///     Data 方言降级到 Standard 方言
    /// </summary>
    public IReadOnlyList<IDialect> lowering_targets => new List<IDialect>
    {
        new StandardDialect()
    };

    /// <summary>
    ///     部分求值工厂列表
    /// </summary>
    public IReadOnlyList<IPEFactory> pe_factories => [];
}