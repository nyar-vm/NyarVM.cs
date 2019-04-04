using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     谓词下推：Filter(Join(A, B, cond1), cond2) -> Join(A, Filter(B, cond2), cond1)
/// </summary>
public sealed class PredicatePushdownRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "predicate-pushdown";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Filter filter) continue;

            var dataClass = egraph.get_class(filter.data);
            if (dataClass is null) continue;

            foreach (var dataNode in dataClass.nodes)
            {
                if (dataNode is not Join join) continue;

                var pushedJoin = new Join(join.type, join.left,
                    egraph.add(new Filter(filter.predicate, join.right)).value, join.condition);

                yield return (node, pushedJoin);
            }
        }
    }
}