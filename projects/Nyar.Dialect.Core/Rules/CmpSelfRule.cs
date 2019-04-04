using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     自身比较规则（OA 节点版）：Cmp(Eq, a, a) → Literal&lt;bool&gt;(true)，Cmp(Ne, a, a) → Literal&lt;bool&gt;(false) 等。
///     使用 Core 方言 OA 节点 <c>Cmp</c>、<c>CompareOp</c>、<c>Literal&lt;bool&gt;</c>。
///     此规则需要 EGraph 级操作（union-find 比较），无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class CmpSelfRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cmp-self";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cmp cmp) continue;

            if (!AreSameRoot(egraph, cmp.left, cmp.right)) continue;

            switch (cmp.op)
            {
                case CompareOp.eq:
                    yield return (node, new Literal<bool>(true));
                    break;
                case CompareOp.ne:
                    yield return (node, new Literal<bool>(false));
                    break;
                case CompareOp.le:
                case CompareOp.ge:
                    yield return (node, new Literal<bool>(true));
                    break;
                case CompareOp.lt:
                case CompareOp.gt:
                    yield return (node, new Literal<bool>(false));
                    break;
            }
        }
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }
}