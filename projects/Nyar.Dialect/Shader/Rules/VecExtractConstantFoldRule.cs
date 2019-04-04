using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     VecExtract 常量折叠：当源向量是常量向量时，直接提取对应分量
/// </summary>
public sealed class VecExtractConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "vec-extract-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not VecExtract extract) continue;

            var vecClass = egraph.get_class(extract.vector);
            var vecNode = vecClass?.nodes.OfType<Vec>().FirstOrDefault();
            if (vecNode is null) continue;

            var constValues = new List<double>();
            var allConstant = true;

            foreach (var compId in vecNode.components)
            {
                var compClass = egraph.get_class(compId);
                var constNode = compClass?.nodes.OfType<Literal<long>>().FirstOrDefault();

                if (constNode is not null)
                {
                    constValues.Add(constNode.value);
                }
                else
                {
                    allConstant = false;
                    break;
                }
            }

            if (!allConstant) continue;

            var indices = SwizzleToIndices(extract.swizzle, vecNode.size);
            if (indices is null) continue;

            if (indices.Count == 1)
            {
                var idx = indices[0];
                if (idx >= 0 && idx < constValues.Count) yield return (node, new Literal<long>((long)constValues[idx]));
            }
            else
            {
                var compIds = new List<Id>();
                foreach (var idx in indices)
                {
                    if (idx < 0 || idx >= constValues.Count)
                    {
                        compIds = null;
                        break;
                    }

                    compIds.Add(FindOrCreateConstant(egraph, constValues[idx]));
                }

                if (compIds is not null) yield return (node, new Vec(indices.Count, compIds));
            }
        }
    }

    private static List<int>? SwizzleToIndices(string swizzle, int vecSize)
    {
        var indices = new List<int>();
        foreach (var ch in swizzle.ToLowerInvariant())
        {
            var idx = ch switch
            {
                'x' or 'r' => 0,
                'y' or 'g' => 1,
                'z' or 'b' => 2,
                'w' or 'a' => 3,
                _ => -1
            };

            if (idx < 0 || idx >= vecSize) return null;
            indices.Add(idx);
        }

        return indices.Count > 0 ? indices : null;
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