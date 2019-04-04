using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Dot 常量折叠：当 Dot 的两个向量都是常量向量时，编译期计算点积
/// </summary>
public sealed class DotConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dot-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Dot dot) continue;

            var leftValues = TryGetVecConstants(egraph, dot.left);
            var rightValues = TryGetVecConstants(egraph, dot.right);

            if (leftValues is null || rightValues is null) continue;
            if (leftValues.Count != rightValues.Count) continue;

            var result = 0.0;
            for (var i = 0; i < leftValues.Count; i++) result += leftValues[i] * rightValues[i];

            yield return (node, new Literal<double>(result));
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
}