using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;
using Nyar.PartialEvaluate;

namespace Nyar.Dialect.Core;

/// <summary>
///     Core 方言是 Nyar IR 的宪法，仅包含 15 个不可再分的语义原子
/// </summary>
public sealed class CoreDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "core";

    /// <summary>
    ///     Core 方言的重写规则（代数等价规则 + 常量折叠 + 死代码消除 + 布尔简化 + 否定规则 + 比较规则）
    /// </summary>
    public IReadOnlyList<IRewriteRule<AlgebraNode>> rules => OaCoreRules.all_rules();

    /// <summary>
    ///     Core 方言的成本模型钩子（含策略感知成本估算）
    /// </summary>
    // OA 重构中：CoreCostHook 已排除编译，暂时禁用
    public IReadOnlyList<ICostModelHook> cost_hooks =>
    [
        // new CoreCostHook(),
        new StrategyCostHook()
    ];

    /// <summary>
    ///     Core 方言是终端方言，无更低层降级目标。
    ///     Standard 方言可通过 PE 降级到 Core 以支持 Native 平台。
    /// </summary>
    public IReadOnlyList<IDialect> lowering_targets => [];

    /// <summary>
    ///     部分求值工厂列表
    /// </summary>
    public IReadOnlyList<IPartialEvaluateFactory> pe_factories => [];
}