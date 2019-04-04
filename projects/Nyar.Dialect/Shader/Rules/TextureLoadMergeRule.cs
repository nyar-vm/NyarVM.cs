using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     重复纹理加载合并：相同纹理+相同坐标的 TextureLoad 只需执行一次
/// </summary>
public sealed class TextureLoadMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "texture-load-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not TextureLoad texLoad) continue;

            foreach (var (_, otherClass) in egraph.classes)
            {
                if (otherClass.id.Equals(id)) continue;

                foreach (var otherNode in otherClass.nodes)
                {
                    if (otherNode is not TextureLoad otherTexLoad) continue;

                    if (!egraph.union_find.find(texLoad.texture)
                            .Equals(egraph.union_find.find(otherTexLoad.texture))) continue;

                    if (!egraph.union_find.find(texLoad.coordinates)
                            .Equals(egraph.union_find.find(otherTexLoad.coordinates))) continue;

                    yield return (node, otherNode);
                    yield break;
                }
            }
        }
    }
}