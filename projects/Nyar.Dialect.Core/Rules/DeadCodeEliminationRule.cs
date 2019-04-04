using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     死代码消除规则（OA 节点版）：Add(x, 0) → x、Sub(x, 0) → x、Mul(x, 1) → x、Mul(x, 0) → 0、Div(x, 1) → x 等。
///     使用 Core 方言 OA 节点 <c>Add</c>、<c>Sub</c>、<c>Mul</c>、<c>Div</c>、<c>Literal&lt;long&gt;</c>。
///     此规则需要等价类内的常量检测（IsZero/IsOne），无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class DeadCodeEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "dead-code-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        AlgebraNode rep;
        foreach (var node in eclass.nodes.ToList())
            switch (node)
            {
                case Add add when IsZero(egraph, add.right):
                    if (TryGetReplacement(egraph, add.left, out rep)) yield return (add, rep);
                    break;
                case Add add when IsZero(egraph, add.left):
                    if (TryGetReplacement(egraph, add.right, out rep)) yield return (add, rep);
                    break;
                case Sub sub when IsZero(egraph, sub.right):
                    if (TryGetReplacement(egraph, sub.left, out rep)) yield return (sub, rep);
                    break;
                case Mul mul when IsOne(egraph, mul.right):
                    if (TryGetReplacement(egraph, mul.left, out rep)) yield return (mul, rep);
                    break;
                case Mul mul when IsOne(egraph, mul.left):
                    if (TryGetReplacement(egraph, mul.right, out rep)) yield return (mul, rep);
                    break;
                case Mul mul when IsZero(egraph, mul.right) || IsZero(egraph, mul.left):
                    yield return (mul, new Literal<long>(0));
                    break;
                case Div div when IsOne(egraph, div.right):
                    if (TryGetReplacement(egraph, div.left, out rep)) yield return (div, rep);
                    break;
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
        if (targetClass is null) return false;

        return targetClass.nodes.Any(n => n is Literal<long> { value: 0 });
    }

    private static bool IsOne(EGraph<AlgebraNode> egraph, Id id)
    {
        var targetClass = egraph.get_class(id);
        if (targetClass is null) return false;

        return targetClass.nodes.Any(n => n is Literal<long> { value: 1 });
    }
}