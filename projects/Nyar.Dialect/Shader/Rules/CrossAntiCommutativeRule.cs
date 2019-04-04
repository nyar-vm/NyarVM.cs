using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Cross 反交换律：Cross(a, b) == -Cross(b, a)
///     等价表达为 Cross(a, b) 与 Neg(Cross(b, a)) 等价
/// </summary>
public sealed class CrossAntiCommutativeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cross-anti-commutative";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cross cross) continue;

            var swapped = new Cross(cross.right, cross.left);
            var swappedId = egraph.add(swapped);
            var negOneId = FindOrCreateConstant(egraph, -1);
            var negated = new Mul(swappedId, negOneId);
            yield return (node, negated);
        }
    }

    private static Id FindOrCreateConstant(EGraph<AlgebraNode> egraph, long value)
    {
        foreach (var (_, eclass) in egraph.classes)
            if (eclass.nodes.OfType<Literal<long>>().Any(c => c.value == value))
                return eclass.id;

        var constNode = new Literal<long>(value);
        return egraph.add(constNode);
    }
}