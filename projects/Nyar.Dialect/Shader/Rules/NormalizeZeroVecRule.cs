using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     Normalize 零向量检测：Normalize(零向量) 是未定义行为，标记为常量零
/// </summary>
public sealed class NormalizeZeroVecRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "normalize-zero-vec";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Normalize normalize) continue;

            var vecClass = egraph.get_class(normalize.vector);
            var vecNode = vecClass?.nodes.OfType<Vec>().FirstOrDefault();

            if (vecNode is null) continue;

            var allZero = true;
            foreach (var compId in vecNode.components)
            {
                var compClass = egraph.get_class(compId);
                var constNode = compClass?.nodes.OfType<Literal<long>>().FirstOrDefault();

                if (constNode is null || constNode.value != 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero) yield return (node, new Literal<long>(0));
        }
    }
}