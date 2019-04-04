using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     平凡过滤消除：Filter(Constant(true), data) → data；Filter(Constant(false), data) → 空集
/// </summary>
public sealed class TrivialFilterRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "trivial-filter";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Filter filter) continue;

            var predClass = egraph.get_class(filter.predicate);
            if (predClass is null) continue;

            foreach (var predNode in predClass.nodes)
                if (predNode is Literal<long> { value: 0 })
                    // TODO: 需定义 Data 方言的 EmptyResult 节点
                    yield return (node, new Literal<long>(-1));
        }
    }
}