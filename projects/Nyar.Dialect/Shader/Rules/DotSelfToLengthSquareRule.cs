using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     向量代数简化：Dot(v, v) → Length(v)²
/// </summary>
public sealed class DotSelfToLengthSquareRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dot-self-to-length-square";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Dot dot) continue;

            if (egraph.union_find.find(dot.left).Equals(egraph.union_find.find(dot.right)))
            {
                var lengthId = FindOrCreateLength(egraph, dot.left);
                var lengthClass = egraph.get_class(lengthId);

                if (lengthClass is not null) yield return (node, new Dot(lengthId, lengthId));
            }
        }
    }

    private static Id FindOrCreateLength(EGraph<AlgebraNode> egraph, Id vectorId)
    {
        foreach (var (_, eclass) in egraph.classes)
        {
            var lengthNode = eclass.nodes.OfType<Length>().FirstOrDefault(l =>
                egraph.union_find.find(l.vector).Equals(egraph.union_find.find(vectorId)));

            if (lengthNode is not null) return eclass.id;
        }

        var newLength = new Length(vectorId);
        return egraph.add(newLength);
    }
}