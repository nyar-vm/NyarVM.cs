using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Data.Rules;

/// <summary>
///     Limit 零消除：Limit(0, data) → 空结果集
/// </summary>
public sealed class LimitZeroRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "limit-zero";

    /// <inheritdoc />
    protected override IEnumerable<(Oa Pattern, Oa Replacement)> apply_to_class(EGraph<Oa> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Limit limit) continue;

            var countClass = egraph.get_class(limit.count);
            if (countClass is null) continue;

            foreach (var countNode in countClass.nodes)
                if (countNode is Literal<long> { value: 0 })
                    yield return (node, new Literal<long>(-1));
        }
    }
}