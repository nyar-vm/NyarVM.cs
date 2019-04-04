using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Neural.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Neural.Rules;

/// <summary>
///     LoRA 权重融合规则：MatMul(x, W) + LoRA(x, W, A, B, alpha) -> MatMul(x, W + A*B*alpha/r)
///     推理模式下将 LoRA 适配器权重融合到基座权重中，消除运行时开销
/// </summary>
public sealed class LoRAFusionRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "lora-fusion";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Add addOp) continue;

            var leftClass = egraph.get_class(addOp.left);
            var rightClass = egraph.get_class(addOp.right);
            if (leftClass is null || rightClass is null) continue;

            foreach (var leftNode in leftClass.nodes)
            {
                if (leftNode is not MatMul baseMatMul) continue;

                foreach (var rightNode in rightClass.nodes)
                {
                    if (rightNode is not LoRA lora) continue;

                    var baseInputClass = egraph.get_class(baseMatMul.left);
                    var loraInputClass = egraph.get_class(lora.input);
                    if (baseInputClass is null || loraInputClass is null) continue;

                    if (!SameEClass(egraph, baseMatMul.left, lora.input)) continue;

                    yield return (node, new LoRA(
                        baseMatMul.left,
                        baseMatMul.right,
                        lora.downProjection,
                        lora.upProjection,
                        lora.alpha));
                }
            }
        }
    }

    private static bool SameEClass(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        var classA = egraph.get_class(a);
        var classB = egraph.get_class(b);
        if (classA is null || classB is null) return false;

        return classA.id == classB.id;
    }
}