using Nyar.Dialect.Core;
using Nyar.Dialect.Standard.Rules;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;
using Nyar.PartialEvaluate;

namespace Nyar.Dialect.Standard;

/// <summary>
///     Standard 方言是 Core 方言的直接扩展，提供所有高级语言共需的基础类型和操作。
///     同时包含策略引入规则，让 EGraph 自动为计算探索优化策略。
/// </summary>
public sealed class StandardDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "std";

    /// <summary>
    ///     Standard 方言的重写规则（含策略引入规则和去虚拟化规则）
    /// </summary>
    public IReadOnlyList<IRewriteRule<AlgebraNode>> rules => StandardRules.all_rewrite_rules();

    // OA 重构中：StandardCostHook 和 StandardToCorePEFactory 尚未迁移至 OA 架构，暂时禁用
    /// <summary>
    ///     Standard 方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> cost_hooks =>
    [
        // new StandardCostHook()
    ];

    /// <summary>
    ///     Standard 方言降级到 Core 方言
    /// </summary>
    public IReadOnlyList<IDialect> lowering_targets =>
    [
        new CoreDialect()
    ];

    /// <summary>
    ///     部分求值工厂列表，用于将 Standard 方言降级到 Core 方言
    /// </summary>
    public IReadOnlyList<IPartialEvaluateFactory> pe_factories =>
    [
        // new StandardToCorePEFactory()
    ];
}