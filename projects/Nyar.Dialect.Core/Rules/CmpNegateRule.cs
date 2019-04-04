using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     比较否定律规则（OA 节点版）：Not(Cmp(Eq, a, b)) → Cmp(Ne, a, b)，Not(Cmp(Lt, a, b)) → Cmp(Ge, a, b) 等。
///     使用 Core 方言 OA 节点 <c>Not</c>、<c>Cmp</c>、<c>CompareOp</c>。
///     此规则需要嵌套等价类遍历（先匹配 Not，再查内层 Cmp），无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class CmpNegateRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cmp-negate";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Not not) continue;

            var innerClass = egraph.get_class(not.operand);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not Cmp cmp) continue;

                var negatedOp = cmp.op switch
                {
                    CompareOp.eq => (CompareOp?)CompareOp.ne,
                    CompareOp.ne => CompareOp.eq,
                    CompareOp.lt => CompareOp.ge,
                    CompareOp.ge => CompareOp.lt,
                    CompareOp.gt => CompareOp.le,
                    CompareOp.le => CompareOp.gt,
                    _ => null
                };

                if (negatedOp is not null) yield return (node, new Cmp(negatedOp.Value, cmp.left, cmp.right));
            }
        }
    }
}