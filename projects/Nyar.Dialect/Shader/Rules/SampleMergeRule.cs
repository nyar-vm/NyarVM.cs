using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     纹理采样合并：相同纹理 + 相同坐标的 Sample 操作只需执行一次
///     类似 TextureLoadMergeRule，但针对带插值的 Sample 节点
/// </summary>
public sealed class SampleMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sample-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Sample sample) continue;

            foreach (var (_, otherClass) in egraph.classes)
            {
                if (otherClass.id.Equals(id)) continue;

                foreach (var otherNode in otherClass.nodes)
                {
                    if (otherNode is not Sample otherSample) continue;

                    if (!egraph.union_find.find(sample.texture)
                            .Equals(egraph.union_find.find(otherSample.texture))) continue;

                    if (!egraph.union_find.find(sample.coordinates)
                            .Equals(egraph.union_find.find(otherSample.coordinates))) continue;

                    yield return (node, otherNode);
                    yield break;
                }
            }
        }
    }
}