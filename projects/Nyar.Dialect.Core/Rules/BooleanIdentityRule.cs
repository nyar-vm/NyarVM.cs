using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     布尔恒等规则（OA 节点版）：Cmp(Eq, a, a) → true，Cmp(Ne, a, a) → false，Branch(const, t, f) → 选择等。
///     使用 Core 方言 OA 节点 <c>Cmp</c>、<c>CompareOp</c>、<c>Branch</c>、<c>Literal&lt;bool&gt;</c>。
///     此规则需要 EGraph 级操作（union-find 比较），无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class BooleanIdentityRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "boolean-identity";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes.ToList())
            switch (node)
            {
                case Cmp { op: CompareOp.eq } cmp:
                {
                    if (AreSameRoot(egraph, cmp.left, cmp.right)) yield return (node, new Literal<bool>(true));

                    break;
                }

                case Cmp { op: CompareOp.ne } cmp:
                {
                    if (AreSameRoot(egraph, cmp.left, cmp.right)) yield return (node, new Literal<bool>(false));

                    break;
                }

                case Branch branch:
                {
                    if (TryGetBoolConstant(egraph, branch.condition, out var condVal))
                    {
                        var targetId = condVal ? branch.trueLabel : branch.falseLabel;
                        if (TryGetReplacement(egraph, targetId, out var rep)) yield return (node, rep);
                    }

                    break;
                }
            }
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }

    /// <summary>
    ///     从指定 Id 的等价类中获取替换节点，直接使用等价类中的现有节点以触发 EGraph 合并
    /// </summary>
    private static bool TryGetReplacement(EGraph<AlgebraNode> egraph, Id id, out AlgebraNode replacement)
    {
        replacement = null!;
        var targetClass = egraph.get_class(id);
        if (targetClass is null || targetClass.nodes.Count == 0) return false;

        // 优先返回 OA 节点，次选旧类型
        replacement = targetClass.nodes.FirstOrDefault(n => n is Literal<long>)
                      ?? targetClass.nodes.First();
        return true;
    }

    private static bool TryGetBoolConstant(EGraph<AlgebraNode> egraph, Id id, out bool value)
    {
        value = false;
        var targetClass = egraph.get_class(id);
        if (targetClass is null) return false;

        foreach (var node in targetClass.nodes)
            if (node is Literal<bool> lit)
            {
                value = lit.value;
                return true;
            }

        return false;
    }
}