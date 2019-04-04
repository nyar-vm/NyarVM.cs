using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     过滤合并：Filter(cond1, Filter(cond2, data)) → Filter(cond1 AND cond2, data)
///     将嵌套过滤条件合并为单一过滤
/// </summary>
public sealed class FilterMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "filter-merge";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Filter outer) continue;

            var innerClass = egraph.get_class(outer.data);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
                if (innerNode is Filter inner)
                {
                    var andCond = new And(outer.predicate, inner.predicate);
                    yield return (node, new Filter(egraph.add(andCond), inner.data));
                }
        }
    }
}