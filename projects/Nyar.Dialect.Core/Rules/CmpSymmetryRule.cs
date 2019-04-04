using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     比较对称律规则（OA 节点版）：Cmp(Lt, a, b) ↔ Cmp(Gt, b, a)，Cmp(Le, a, b) ↔ Cmp(Ge, b, a)。
///     使用 Core 方言 OA 节点 <c>Cmp</c>、<c>CompareOp</c>。
///     此规则需要遍历等价类中的所有节点，无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class CmpSymmetryRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cmp-symmetry";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cmp cmp) continue;

            var swapped = cmp.op switch
            {
                CompareOp.lt => (CompareOp?)CompareOp.gt,
                CompareOp.gt => CompareOp.lt,
                CompareOp.le => CompareOp.ge,
                CompareOp.ge => CompareOp.le,
                _ => null
            };

            if (swapped is not null) yield return (node, new Cmp(swapped.Value, cmp.right, cmp.left));
        }
    }
}