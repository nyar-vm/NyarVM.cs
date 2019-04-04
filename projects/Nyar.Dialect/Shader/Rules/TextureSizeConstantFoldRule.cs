using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     TextureSize 常量折叠：当 LOD 为常量 0 且纹理尺寸已知时，编译期计算尺寸
/// </summary>
public sealed class TextureSizeConstantFoldRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "texture-size-constant-fold";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
            if (node is TextureSize texSize)
            {
                var lodClass = egraph.get_class(texSize.lod);
                var lodConst = lodClass?.nodes.OfType<Literal<long>>().FirstOrDefault();

                if (lodConst is not null && lodConst.value == 0)
                {
                    var texClass = egraph.get_class(texSize.texture);
                    var combinedSampler = texClass?.nodes.OfType<CombinedImageSampler>().FirstOrDefault();

                    if (combinedSampler is not null)
                    {
                        var compIds = new List<Id>
                        {
                            FindOrCreateConstant(egraph, 1),
                            FindOrCreateConstant(egraph, 1)
                        };

                        yield return (node, new Vec(2, compIds));
                    }
                }
            }
    }

    private static Id FindOrCreateConstant(EGraph<AlgebraNode> egraph, long value)
    {
        foreach (var (_, eclass) in egraph.classes)
            if (eclass.nodes.OfType<Literal<long>>().Any(c => c.value == value))
                return eclass.id;

        var constNode = new Literal<long>(value);
        return egraph.add(constNode);
    }
}