using Nyar.Dialect.Shader.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Shader.Rules;

/// <summary>
///     无副作用存储消除：当 StorageLoad 的结果从未被使用时，消除该加载
/// </summary>
public sealed class UnusedLoadEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "unused-load-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not (StorageLoad or SharedLoad or UniformLoad)) continue;

            var parents = FindParentNodes(egraph, id);
            var isUsed = parents.Count > 0;

            if (!isUsed) yield return (node, new Nop());
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