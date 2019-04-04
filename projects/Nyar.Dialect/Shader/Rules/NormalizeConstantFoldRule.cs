using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Normalize 常量折叠：当向量的所有分量都是常量时，编译期计算归一化结果
/// </summary>
public sealed class NormalizeConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "normalize-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Normalize normalize) continue;

            var values = TryGetVecConstants(egraph, normalize.vector);
            if (values is null) continue;

            var sumSquares = 0.0;
            foreach (var v in values) sumSquares += v * v;

            if (sumSquares == 0) continue;

            var len = Math.Sqrt(sumSquares);
            var compIds = new List<Id>();

            foreach (var v in values) compIds.Add(FindOrCreateConstant(egraph, v / len));

            yield return (node, new Vec(values.Count, compIds));
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