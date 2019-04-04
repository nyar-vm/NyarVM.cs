using Nyar.IR.Rewrite;
using Nyar.Optimizer.CostModels;

namespace Nyar.ObjectAlgebra;

/// <summary>
///     方言运行时接口，包含方言的静态定义和运行时行为。
///     规则、成本钩子、降级目标在此层注册。
/// </summary>
public interface IDialectRuntime
{
    /// <summary>
    ///     方言静态定义
    /// </summary>
    IDialectDefinition definition { get; }

    /// <summary>
    ///     方言的重写规则集
    /// </summary>
    IReadOnlyList<IRewriteRule<ENode>> rules { get; }

    /// <summary>
    ///     方言的成本模型钩子
    /// </summary>
    IReadOnlyList<ICostModelHook> cost_hooks { get; }

    /// <summary>
    ///     降级目标方言名称列表
    /// </summary>
    IReadOnlyList<string> lowering_targets { get; }
}