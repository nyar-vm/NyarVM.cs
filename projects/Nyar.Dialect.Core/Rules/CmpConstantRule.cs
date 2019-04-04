using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     比较常量折叠规则（OA 节点版）：Cmp(Eq, const1, const2) → BooleanConstant(result) 等。
///     使用 Core 方言 OA 节点 <c>Cmp</c>、<c>CompareOp</c>、<c>Literal&lt;long&gt;</c>。
///     此规则需要等价类内的双侧常量检测，无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class CmpConstantRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cmp-constant";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cmp cmp) continue;

            if (!TryGetConstant(egraph, cmp.left, out var leftVal)) continue;

            if (!TryGetConstant(egraph, cmp.right, out var rightVal)) continue;

            var result = cmp.op switch
            {
                CompareOp.eq => leftVal == rightVal,
                CompareOp.ne => leftVal != rightVal,
                CompareOp.lt => leftVal < rightVal,
                CompareOp.le => leftVal <= rightVal,
                CompareOp.gt => leftVal > rightVal,
                CompareOp.ge => leftVal >= rightVal,
                _ => (bool?)null
            };

            if (result is not null) yield return (node, new Literal<bool>(result.Value));
        }
    }

    private static bool TryGetConstant(EGraph<AlgebraNode> egraph, Id id, out long value)
    {
        value = 0;
        var eclass = egraph.get_class(id);
        if (eclass is null) return false;

        foreach (var node in eclass.nodes)
            if (node is Literal<long> lit)
            {
                value = lit.value;
                return true;
            }

        return false;
    }
}