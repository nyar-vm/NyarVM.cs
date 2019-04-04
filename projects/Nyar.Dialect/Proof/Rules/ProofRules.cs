using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using ReflNode = Nyar.Dialect.Proof.Nodes.Refl;
using TransNode = Nyar.Dialect.Proof.Nodes.Trans;

namespace Nyar.Dialect.Proof.Rules;

/// <summary>
///     自反性简化：Refl(t) 在等价推理中可消除
/// </summary>
public sealed class ReflSimplificationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "refl-simplification";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not TransNode trans) continue;

            var proof1Class = egraph.get_class(trans.proof1);
            if (proof1Class is null) continue;

            foreach (var proof1Node in proof1Class.nodes)
            {
                if (proof1Node is not ReflNode) continue;

                var proof2Class = egraph.get_class(trans.proof2);
                if (proof2Class is null) continue;

                foreach (var proof2Node in proof2Class.nodes)
                {
                    yield return (node, proof2Node);
                    yield break;
                }
            }
        }
    }
}

/// <summary>
///     传递性消除：Trans(Trans(a, b), c) -> Trans(a, Trans(b, c))
/// </summary>
public sealed class TransEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "trans-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not TransNode outerTrans) continue;

            var proof1Class = egraph.get_class(outerTrans.proof1);
            if (proof1Class is null) continue;

            foreach (var proof1Node in proof1Class.nodes)
            {
                if (proof1Node is not TransNode innerTrans) continue;

                var newTrans = egraph.add(new TransNode(innerTrans.proof2, outerTrans.proof2)).value;
                yield return (node, new TransNode(innerTrans.proof1, newTrans));
            }
        }
    }
}