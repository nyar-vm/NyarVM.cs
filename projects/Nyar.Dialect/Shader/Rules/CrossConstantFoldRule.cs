using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Cross 常量折叠：当 Cross 的两个向量都是常量向量时，编译期计算叉积
/// </summary>
public sealed class CrossConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cross-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cross cross) continue;

            var leftValues = TryGetVecConstants(egraph, cross.left);
            var rightValues = TryGetVecConstants(egraph, cross.right);

            if (leftValues is null || rightValues is null) continue;
            if (leftValues.Count < 3 || rightValues.Count < 3) continue;

            var rx = leftValues[1] * rightValues[2] - leftValues[2] * rightValues[1];
            var ry = leftValues[2] * rightValues[0] - leftValues[0] * rightValues[2];
            var rz = leftValues[0] * rightValues[1] - leftValues[1] * rightValues[0];

            var compIds = new List<Id>
            {
                FindOrCreateConstant(egraph, rx),
                FindOrCreateConstant(egraph, ry),
                FindOrCreateConstant(egraph, rz)
            };

            yield return (node, new Vec(3, compIds));
        }
    }

    private static List<double>? TryGetVecConstants(EGraph<AlgebraNode> egraph, Id vecId)
    {
        var vecClass = egraph.get_class(vecId);
        var vecNode = vecClass?.nodes.OfType<Vec>().FirstOrDefault();
        if (vecNode is null) return null;

        var values = new List<double>();

        foreach (var compId in vecNode.components)
        {
            var compClass = egraph.get_class(compId);
            var constNode = compClass?.nodes.OfType<Literal<long>>().FirstOrDefault();
            if (constNode is null) return null;

            values.Add(constNode.value);
        }

        return values;
    }

    private static Id FindOrCreateConstant(EGraph<AlgebraNode> egraph, double value)
    {
        var longValue = (long)Math.Round(value);

        foreach (var (_, eclass) in egraph.classes)
            if (eclass.nodes.OfType<Literal<long>>().Any(c => c.value == longValue))
                return eclass.id;

        var constNode = new Literal<long>(longValue);
        return egraph.add(constNode);
    }
}