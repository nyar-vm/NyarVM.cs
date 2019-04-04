using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.Optimizer.CostModels;
using Nyar.PartialEvaluate;

namespace Nyar.ObjectAlgebra;

/// <summary>
///     方言接口，每个领域方言必须实现此接口
/// </summary>
/// <remarks>
///     ⛔ 此接口依赖 AlgebraNode 封闭节点宇宙，已冻结。
///     新的方言应实现 IDialectRuntime 接口，使用开放 ENode 模型。
///     详见 .trae/specs/oa-full-pipeline/spec.md。
/// </remarks>
[Obsolete("IDialect 依赖 AlgebraNode 封闭节点宇宙，已冻结。请使用 IDialectRuntime + IDialectDefinition 开放模型")]
public interface IDialect
{
    /// <summary>
    ///     方言名称（全局唯一，作为方言标识符）
    /// </summary>
    string name { get; }

    /// <summary>
    ///     方言的重写规则集
    /// </summary>
    IReadOnlyList<IRewriteRule<AlgebraNode>> rules { get; }

    /// <summary>
    ///     方言的成本模型钩子
    /// </summary>
    IReadOnlyList<ICostModelHook> cost_hooks { get; }

    /// <summary>
    ///     降级目标方言列表
    /// </summary>
    IReadOnlyList<IDialect> lowering_targets { get; }

    /// <summary>
    ///     PE（部分求值）工厂集合，用于将本方言降级为目标方言
    /// </summary>
    IReadOnlyList<IPartialEvaluateFactory> pe_factories { get; }
}

/// <summary>
///     ⛔ 泛型方言接口（向后兼容别名），已冻结。
///     此接口依赖 AlgebraNode 封闭节点宇宙，请使用 <see cref="IDialectRuntime" /> 开放模型。
/// </summary>
/// <typeparam name="T">节点类型，必须为 AlgebraNode（已冻结）</typeparam>
[Obsolete("IDialect<T> 依赖 AlgebraNode 封闭节点宇宙，已冻结。请使用 IDialectRuntime + IDialectDefinition 开放模型")]
public interface IDialect<T> : IDialect where T : AlgebraNode
{
}