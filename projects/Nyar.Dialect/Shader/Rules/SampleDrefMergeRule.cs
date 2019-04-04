using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     SampleDref 合并：相同纹理 + 相同坐标 + 相同深度参考值的 SampleDref 操作只需执行一次
/// </summary>
public sealed class SampleDrefMergeRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sample-dref-merge";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not SampleDref sampleDref) continue;

            foreach (var (_, otherClass) in egraph.classes)
            {
                if (otherClass.id.Equals(id)) continue;

                foreach (var otherNode in otherClass.nodes)
                {
                    if (otherNode is not SampleDref otherDref) continue;

                    if (!egraph.union_find.find(sampleDref.texture)
                            .Equals(egraph.union_find.find(otherDref.texture))) continue;

                    if (!egraph.union_find.find(sampleDref.coordinates)
                            .Equals(egraph.union_find.find(otherDref.coordinates))) continue;

                    if (!egraph.union_find.find(sampleDref.depth_ref)
                            .Equals(egraph.union_find.find(otherDref.depth_ref))) continue;

                    yield return (node, otherNode);
                    yield break;
                }
            }
        }
    }
}