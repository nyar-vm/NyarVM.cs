using Nyar.Dialect.Core;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.ObjectAlgebra;
using Nyar.Optimizer.CostModels;

namespace Nyar.Dialect.Neural;

/// <summary>
///     Tensor 方言面向深度学习与科学计算领域
///     包含基础算子（Conv2D/MatMul/Softmax 等）和 Transformer/LLM 算子（FusedAttention/RmsNorm/RoPE 等）
/// </summary>
public sealed class TensorDialect : IDialect
{
    /// <summary>
    ///     方言名称
    /// </summary>
    public string name => "tensor";

    /// <summary>
    ///     Tensor 方言的重写规则
    /// </summary>
    public IReadOnlyList<IRewriteRule<Oa>> rules => new List<IRewriteRule<Oa>>();

    /// <summary>
    ///     Tensor 方言的成本模型钩子
    /// </summary>
    public IReadOnlyList<ICostModelHook> cost_hooks => new List<ICostModelHook>();

    /// <summary>
    ///     Tensor 方言降级到 Core 方言
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