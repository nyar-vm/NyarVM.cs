using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     分组合并：GroupBy(keys1, aggs1, GroupBy(keys2, aggs2, data))
///     当 keys2 ⊇ keys1 时，外层 GroupBy 可合并到内层
/// </summary>
public sealed class GroupByMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "groupby-merge";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not GroupBy outer) continue;

            var innerClass = egraph.get_class(outer.data);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not GroupBy inner) continue;

                if (IsSuperset(inner.keys, outer.keys))
                {
                    var mergedAggs = inner.aggregates.Concat(outer.aggregates).ToList();
                    yield return (node, new GroupBy(inner.keys, mergedAggs, inner.data));
                }
            }
        }
    }

    /// <summary>
    ///     检查 superset 是否包含 subset 的所有键
    /// </summary>
    private static bool IsSuperset(IReadOnlyList<string> superset, IReadOnlyList<string> subset)
    {
        var superSet = new HashSet<string>(superset);
        return subset.All(superSet.Contains);
    }
}