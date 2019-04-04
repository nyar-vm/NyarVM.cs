using Nyar.Dialect.Core;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;

namespace Nyar.Dialect.Proof;

/// <summary>
///     Proof 方言用于形式化验证与定理证明
/// </summary>
public sealed class ProofDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "proof";

    /// <summary>
    ///     Proof 方言的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<Oa>> rules => new List<IRewriteRule<Oa>>();

    /// <summary>
    ///     Proof 方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> cost_hooks => new List<ICostModelHook>();

    /// <summary>
    ///     Proof 方言降级到 Core 方言
    /// </summary>
    public IReadOnlyList<IDialect> lowering_targets => new List<IDialect>
    {
        new CoreDialect()
    };

    /// <summary>
    ///     部分求值工厂列表
    /// </summary>
    public IReadOnlyList<IPEFactory> pe_factories => [];
}