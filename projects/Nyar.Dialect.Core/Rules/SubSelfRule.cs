using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     自身相减归零：Sub(a, a) → Constant(0)
/// </summary>
public sealed class SubSelfRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sub-self";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Sub sub) continue;

            if (AreSameRoot(egraph, sub.left, sub.right)) yield return (node, new Literal<long>(0));
        }
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }
}