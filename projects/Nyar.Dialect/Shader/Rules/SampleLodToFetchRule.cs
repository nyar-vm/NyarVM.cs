using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     SampleLod 降级为 TextureLoad：当 LOD 为 0 且坐标为整数时，SampleLod 等价于 TextureLoad
/// </summary>
public sealed class SampleLodToFetchRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sample-lod-to-fetch";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not SampleLod sampleLod) continue;

            var lodClass = egraph.get_class(sampleLod.lod);
            var lodConst = lodClass?.nodes.OfType<Literal<long>>().FirstOrDefault();

            if (lodConst is not null && lodConst.value == 0)
            {
                var coordClass = egraph.get_class(sampleLod.coordinates);
                var isIntegerCoord = coordClass?.nodes.OfType<Literal<long>>().Any() ?? false;

                if (isIntegerCoord) yield return (node, new TextureLoad(sampleLod.texture, sampleLod.coordinates));
            }
        }
    }
}