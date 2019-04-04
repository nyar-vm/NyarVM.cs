using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Reflect 常量折叠：当入射向量和法线都是常量向量时，编译期计算反射向量
///     Reflect(I, N) = I - 2 * Dot(I, N) * N
/// </summary>
public sealed class ReflectConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "reflect-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Reflect reflect) continue;

            var incidentValues = TryGetVecConstants(egraph, reflect.incident);
            var normalValues = TryGetVecConstants(egraph, reflect.normal);

            if (incidentValues is null || normalValues is null) continue;
            if (incidentValues.Count != normalValues.Count) continue;

            var dot = 0.0;
            for (var i = 0; i < incidentValues.Count; i++) dot += incidentValues[i] * normalValues[i];

            var compIds = new List<Id>();
            for (var i = 0; i < incidentValues.Count; i++)
            {
                var reflected = incidentValues[i] - 2 * dot * normalValues[i];
                compIds.Add(FindOrCreateConstant(egraph, reflected));
            }

            yield return (node, new Vec(incidentValues.Count, compIds));
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