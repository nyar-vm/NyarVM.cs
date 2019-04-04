using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Length 常量折叠：当向量的所有分量都是常量时，编译期计算长度
/// </summary>
public sealed class LengthConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "length-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Length length) continue;

            var values = TryGetVecConstants(egraph, length.vector);
            if (values is null) continue;

            var sumSquares = 0.0;
            foreach (var v in values) sumSquares += v * v;

            var result = Math.Sqrt(sumSquares);
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