using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     SampleLod 合并：相同纹理 + 相同坐标 + 相同 LOD 的 SampleLod 操作只需执行一次
/// </summary>
public sealed class SampleLodMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sample-lod-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not SampleLod sampleLod) continue;

            foreach (var (_, otherClass) in egraph.classes)
            {
                if (otherClass.id.Equals(id)) continue;

                foreach (var otherNode in otherClass.nodes)
                {
                    if (otherNode is not SampleLod otherLod) continue;

                    if (!egraph.union_find.find(sampleLod.texture)
                            .Equals(egraph.union_find.find(otherLod.texture))) continue;

                    if (!egraph.union_find.find(sampleLod.coordinates)
                            .Equals(egraph.union_find.find(otherLod.coordinates))) continue;

                    if (!egraph.union_find.find(sampleLod.lod)
                            .Equals(egraph.union_find.find(otherLod.lod))) continue;

                    yield return (node, otherNode);
                    yield break;
                }
            }
        }
    }
}