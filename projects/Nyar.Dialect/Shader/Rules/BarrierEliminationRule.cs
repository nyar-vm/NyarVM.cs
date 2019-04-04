using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Barrier = Nyar.Dialect.Shader.Nodes.Barrier;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     冗余屏障消除：连续相同作用域的屏障可合并为单个屏障
/// </summary>
public sealed class BarrierEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "barrier-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Barrier barrier) continue;

            var parents = FindParentNodes(egraph, id);
            var redundantParentBarriers = parents
                .Where(p => p is Barrier b && b.memory_scope == barrier.memory_scope)
                .ToList();

            if (redundantParentBarriers.Count > 0) yield return (node, new Nop());
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