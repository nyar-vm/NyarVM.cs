using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     除法分配律（因子提取）：Div(Add(a, b), c) → Add(Div(a, c), Div(b, c))
/// </summary>
public sealed class DivDistributiveRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "div-distributive";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Div div) continue;

            var leftClass = egraph.get_class(div.left);
            if (leftClass is null) continue;

            foreach (var leftNode in leftClass.nodes)
                if (leftNode is Add add)
                {
                    var divLeft = egraph.add(new Div(add.left, div.right)).value;
                    var divRight = egraph.add(new Div(add.right, div.right)).value;
                    yield return (node, new Add(divLeft, divRight));
                }
        }
    }
}