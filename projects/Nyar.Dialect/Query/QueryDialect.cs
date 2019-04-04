using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;

namespace Nyar.Dialect.Query;

/// <summary>
///     Query 方言，定义查询计划代数。降级目标待 CoreDialect 从 Oak 迁移至 Nyar 后指定。
/// </summary>
public sealed class QueryDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "query";

    /// <summary>
    ///     Query 方言的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<AlgebraNode>> rules => new List<IRewriteRule<AlgebraNode>>();

    /// <summary>
    ///     Query 方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> cost_hooks => new List<ICostModelHook>();

    /// <summary>
    ///     降级目标方言（TODO: 指向 CoreDialect，待其从 Oak 迁移至 Nyar）
    /// </summary>
    public IReadOnlyList<IDialect> lowering_targets => new List<IDialect>();

    /// <summary>
    ///     部分求值工厂列表
    /// </summary>
    public IReadOnlyList<IPEFactory> pe_factories => [];
}