using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     纹理采样降级：将 Sample 转换为 TextureLoad + 手动过滤（用于不需要插值的场景）
/// </summary>
public sealed class SampleToFetchRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sample-to-fetch";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Sample sample) continue;

            var coordClass = egraph.get_class(sample.coordinates);
            var isIntegerCoord = coordClass?.nodes.OfType<Literal<long>>().Any() ?? false;

            if (isIntegerCoord) yield return (node, new TextureLoad(sample.texture, sample.coordinates));
        }
    }
}