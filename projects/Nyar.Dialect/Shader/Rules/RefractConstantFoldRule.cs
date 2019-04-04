using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Refract 常量折叠：当入射向量、法线和折射率都是常量时，编译期计算折射向量
///     Refract(I, N, eta) = eta * I + (eta * Dot(N, I) - k) * N
///     其中 k = 1 - eta^2 * (1 - Dot(N, I)^2)，当 k < 0 时返回零向量
/// </summary>
public sealed class RefractConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "refract-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Refract refract) continue;

            var incidentValues = TryGetVecConstants(egraph, refract.incident);
            var normalValues = TryGetVecConstants(egraph, refract.normal);
            var etaClass = egraph.get_class(refract.eta);
            var etaConst = etaClass?.nodes.OfType<Literal<long>>().FirstOrDefault();

            if (incidentValues is null || normalValues is null || etaConst is null) continue;
            if (incidentValues.Count != normalValues.Count) continue;

            var eta = etaConst.value;
            var dotNI = 0.0;
            for (var i = 0; i < incidentValues.Count; i++) dotNI += normalValues[i] * incidentValues[i];

            var k = 1.0 - eta * eta * (1.0 - dotNI * dotNI);

            if (k < 0)
            {
                var zeroIds = new List<Id>();
                for (var i = 0; i < incidentValues.Count; i++) zeroIds.Add(FindOrCreateConstant(egraph, 0));

                yield return (node, new Vec(incidentValues.Count, zeroIds));
            }
            else
            {
                var sqrtK = Math.Sqrt(k);
                var compIds = new List<Id>();
                for (var i = 0; i < incidentValues.Count; i++)
                {
                    var refractedComp = eta * incidentValues[i] + (eta * dotNI - sqrtK) * normalValues[i];
                    compIds.Add(FindOrCreateConstant(egraph, refractedComp));
                }

                yield return (node, new Vec(incidentValues.Count, compIds));
            }
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