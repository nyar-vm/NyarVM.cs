using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Dot 交换律：Dot(a, b) == Dot(b, a)
/// </summary>
public sealed class DotCommutativeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dot-commutative";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Dot dot) continue;

            yield return (node, new Dot(dot.right, dot.left));
        }
    }
}