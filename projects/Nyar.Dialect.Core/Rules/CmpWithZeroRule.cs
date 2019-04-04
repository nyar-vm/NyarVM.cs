using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     比较与零规则（OA 节点版）：Cmp(Eq, a, 0) → Not(a)，Cmp(Ne, a, 0) → a。
///     使用 Core 方言 OA 节点 <c>Cmp</c>、<c>CompareOp</c>、<c>Not</c>。
///     此规则需要等价类内的常量检测和嵌套 eclass 查找，无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class CmpWithZeroRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "cmp-with-zero";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Cmp cmp) continue;

            if (!IsZero(egraph, cmp.right)) continue;

            switch (cmp.op)
            {
                case CompareOp.eq:
                {
                    var notNode = new Not(cmp.left);
                    egraph.add(notNode);
                    yield return (node, notNode);
                    break;
                }
                case CompareOp.ne:
                {
                    if (TryGetReplacement(egraph, cmp.left, out var rep)) yield return (node, rep);
                    break;
                }
            }
        }
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

    private static bool IsZero(EGraph<AlgebraNode> egraph, Id id)
    {
        var targetClass = egraph.get_class(id);
        return targetClass?.nodes.Any(n => n is Literal<long> { value: 0 }) ?? false;
    }
}