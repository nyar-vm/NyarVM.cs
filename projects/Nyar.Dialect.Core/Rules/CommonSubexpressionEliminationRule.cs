using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     公共子表达式消除规则
/// </summary>
public sealed class CommonSubexpressionEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "common-subexpression-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        var nodeHashes = new Dictionary<string, List<Id>>();

        foreach (var (classId, otherClass) in egraph.classes)
        foreach (var node in otherClass.nodes)
        {
            var key = ComputeKey(egraph, node);
            if (key is null) continue;

            if (!nodeHashes.TryGetValue(key, out var ids))
            {
                ids = [];
                nodeHashes[key] = ids;
            }

            var rootId = egraph.union_find.find(new Id(classId));
            if (!ids.Any(existing => egraph.union_find.find(existing) == rootId)) ids.Add(new Id(classId));
        }

        foreach (var (key, ids) in nodeHashes)
        {
            if (ids.Count < 2) continue;

            for (var i = 1; i < ids.Count; i++)
            {
                var root1 = egraph.union_find.find(ids[0]);
                var root2 = egraph.union_find.find(ids[i]);

                if (root1 != root2)
                {
                    egraph.union(root1, root2);
                    yield return (new Literal<object?>(null), new Literal<object?>(null));
                }
            }
        }
    }

    private static string? ComputeKey(EGraph<AlgebraNode> egraph, AlgebraNode node)
    {
        return node switch
        {
            Add add => $"Add({CR(egraph, add.left, add.right)})",
            Sub sub => $"Sub({R(egraph, sub.left)},{R(egraph, sub.right)})",
            Mul mul => $"Mul({CR(egraph, mul.left, mul.right)})",
            Div div => $"Div({R(egraph, div.left)},{R(egraph, div.right)})",
            Rem rem => $"Rem({R(egraph, rem.left)},{R(egraph, rem.right)})",
            Cmp cmp => $"Cmp.{cmp.op}({R(egraph, cmp.left)},{R(egraph, cmp.right)})",
            Neg neg => $"Neg({R(egraph, neg.operand)})",
            Not not => $"Not({R(egraph, not.operand)})",
            Load load => $"Load({R(egraph, load.pointer)})",
            Call call =>
                $"Call({R(egraph, call.function)},[{string.Join(",", call.arguments.Select(a => R(egraph, a)))}])",
            _ => null
        };
    }

    private static uint R(EGraph<AlgebraNode> egraph, Id id)
    {
        return egraph.union_find.find(id).value;
    }

    /// <summary>
    ///     交换律感知的二元操作子节点排序，用于 CSE 键计算
    /// </summary>
    private static string CR(EGraph<AlgebraNode> egraph, Id left, Id right)
    {
        var lv = R(egraph, left);
        var rv = R(egraph, right);
        if (lv <= rv) return $"{lv},{rv}";

        return $"{rv},{lv}";
    }
}