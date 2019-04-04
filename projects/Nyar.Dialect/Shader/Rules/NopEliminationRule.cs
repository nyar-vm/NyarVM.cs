using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     死代码消除：移除无副作用的 Nop 节点
/// </summary>
public sealed class NopEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "nop-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
            if (node is Nop)
            {
                var hasSideEffectParent = FindParentNodes(egraph, id)
                    .Any(p => p is ComputeKernel or StorageStore or SharedStore
                        or AtomicAdd or AtomicExchange or AtomicCompareExchange
                        or TextureStore);

                if (!hasSideEffectParent) yield return (node, new Literal<long>(0));
            }
    }

    private static List<AlgebraNode> FindParentNodes(EGraph<AlgebraNode> egraph, Id childId)
    {
        var parents = new List<AlgebraNode>();

        foreach (var (_, eclass) in egraph.classes)
        foreach (var node in eclass.nodes)
        foreach (var child in node.child_ids())
            if (egraph.union_find.find(child).Equals(egraph.union_find.find(childId)))
            {
                parents.Add(node);
                break;
            }

        return parents;
    }
}