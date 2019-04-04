using Nyar.Dialect.Core;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;

namespace Nyar.Dialect.Game;

/// <summary>
///     Game 方言面向游戏 AI、行为树、决策逻辑、游戏状态管理与 ECS 实体组件系统
/// </summary>
public sealed class GameDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "game";

    /// <summary>
    ///     Game 方言的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<Oa>> rules => new List<IRewriteRule<Oa>>();

    /// <summary>
    ///     Game 方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> cost_hooks => new List<ICostModelHook>();

    /// <summary>
    ///     Game 方言降级到 Core 方言
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