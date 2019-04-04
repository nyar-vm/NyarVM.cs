using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     De Morgan 律规则（OA 节点版）：Not(Cmp(Lt, a, b)) → Cmp(Ge, a, b) 等比较运算的逻辑否定等价变换。
///     使用 Core 方言 OA 节点 <c>Not</c>、<c>Cmp</c>、<c>CompareOp</c>。
///     此规则需要嵌套等价类遍历（先匹配 Not，再查内层 Cmp），无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class DeMorganRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "de-morgan";

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
                switch (innerNode)
                {
                    case Cmp cmp:
                    {
                        var negatedOp = cmp.op switch
                        {
                            CompareOp.lt => CompareOp.ge,
                            CompareOp.le => CompareOp.gt,
                            CompareOp.gt => CompareOp.le,
                            CompareOp.ge => CompareOp.lt,
                            _ => (CompareOp?)null
                        };

                        if (negatedOp is not null) yield return (node, new Cmp(negatedOp.Value, cmp.left, cmp.right));

                        break;
                    }
                }
        }
    }
}