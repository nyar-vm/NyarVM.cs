using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     Join 交换律：Join(A, B, cond) == Join(B, A, cond)
/// </summary>
public sealed class JoinCommutativeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "join-commutative";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Join { type: JoinType.inner } join) continue;

            yield return (node, new Join(JoinType.inner, join.right, join.left, join.condition));
        }
    }
}