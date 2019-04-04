using Nyar.Dialect.Neural.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Neural.Rules;

/// <summary>
///     卷积批归一化融合：BatchNorm(Conv2D(x, w), ...) -> FusedConvBnRelu(x, w, ...)
/// </summary>
public sealed class ConvBnFusionRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "conv-bn-fusion";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not BatchNorm bn) continue;

            var inputClass = egraph.get_class(bn.input);
            if (inputClass is null) continue;

            foreach (var inputNode in inputClass.nodes)
            {
                if (inputNode is not Conv2D conv) continue;

                yield return (node,
                    new FusedConvBnRelu(conv.input, conv.weights, conv.bias, bn.scale, bn.bias, bn.runningMean,
                        bn.runningVar, bn.epsilon));
            }
        }
    }
}