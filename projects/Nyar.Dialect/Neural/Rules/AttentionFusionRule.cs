using Nyar.Dialect.Neural.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Neural.Rules;

/// <summary>
///     注意力融合规则：Softmax(MatMul(Q, K^T)) * V -> FusedAttention
///     将 QKV 投影、缩放、Softmax 和 Dropout 融合为单一 FusedAttention 算子
///     推理模式下启用，减少中间张量和内核启动开销
/// </summary>
public sealed class AttentionFusionRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "attention-fusion";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not MatMul attentionOutput) continue;

            var leftClass = egraph.get_class(attentionOutput.left);
            if (leftClass is null) continue;

            foreach (var leftNode in leftClass.nodes)
            {
                if (leftNode is not Softmax softmax) continue;

                var softmaxInputClass = egraph.get_class(softmax.input);
                if (softmaxInputClass is null) continue;

                foreach (var softmaxInputNode in softmaxInputClass.nodes)
                {
                    if (softmaxInputNode is not MatMul qkMatMul) continue;

                    var queryClass = egraph.get_class(qkMatMul.left);
                    var keyClass = egraph.get_class(qkMatMul.right);
                    var valueClass = egraph.get_class(attentionOutput.right);
                    if (queryClass is null || keyClass is null || valueClass is null) continue;

                    foreach (var queryNode in queryClass.nodes)
                    {
                        if (queryNode is not MatMul queryProj) continue;

                        foreach (var keyNode in keyClass.nodes)
                        {
                            if (keyNode is not MatMul keyProj) continue;

                            foreach (var valueNode in valueClass.nodes)
                            {
                                if (valueNode is not MatMul valueProj) continue;

                                yield return (node, new FusedAttention(
                                    qkMatMul.left,
                                    qkMatMul.right,
                                    attentionOutput.right,
                                    null,
                                    0,
                                    0,
                                    null));
                            }
                        }
                    }
                }
            }
        }
    }
}