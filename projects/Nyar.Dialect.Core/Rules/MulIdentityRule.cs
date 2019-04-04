using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     乘法幂等规则：Mul(a, a) → Mul(a, Constant(2)) 的变体
///     以及 Mul(a, Constant(-1)) → Neg(a)
/// </summary>
public sealed class MulIdentityRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "mul-identity";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Mul mul) continue;

            if (IsMinusOne(egraph, mul.right))
            {
                yield return (node, new Neg(mul.left));
                continue;
            }

            if (IsMinusOne(egraph, mul.left))
            {
                yield return (node, new Neg(mul.right));
                continue;
            }

            if (AreSameRoot(egraph, mul.left, mul.right))
            {
                var twoId = egraph.add(new Literal<long>(2)).value;
                yield return (node, new Mul(mul.left, twoId));
            }
        }
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }

    private static bool IsMinusOne(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.Any(n => n is Literal<long> { value: -1 }) ?? false;
    }
}