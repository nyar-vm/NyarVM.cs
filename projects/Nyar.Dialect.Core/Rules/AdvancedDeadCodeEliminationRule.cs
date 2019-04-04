using System.Collections.Immutable;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;
using Tuple = Nyar.Dialect.Core.Nodes.Tuple;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     高级死代码消除规则（OA 节点版）：消除不可达代码、未使用的存储、冗余 Phi 节点。
///     包括 Phi 单值简化、Store-after-Load 消除、a+a → 2*a、a*(-1) → -a、Neg(const) → -const 等。
///     使用 Core 方言 OA 节点 <c>Phi</c>、<c>Store</c>、<c>Load</c>、<c>Add</c>、<c>Mul</c>、<c>Neg</c>、<c>Tuple</c>、<c>ArrayLit</c>、
///     <c>Literal&lt;long&gt;</c>。
///     此规则需要等价类内多类型节点的全局分析，无法转换为 <see cref="RewriteRuleAttribute" /> 属性模式。
/// </summary>
public sealed class AdvancedDeadCodeEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "advanced-dead-code-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes.ToList())
            switch (node)
            {
                case Phi phi:
                {
                    var simplified = TrySimplifyPhi(egraph, phi);
                    if (simplified is not null) yield return (node, simplified);

                    break;
                }

                case Store store:
                {
                    var simplified = TrySimplifyStore(egraph, store);
                    if (simplified is not null) yield return (node, simplified);

                    break;
                }

                case Add add:
                {
                    var simplified = TrySimplifyAdd(egraph, add);
                    if (simplified is not null) yield return (node, simplified);

                    break;
                }

                case Mul mul:
                {
                    var simplified = TrySimplifyMul(egraph, mul);
                    if (simplified is not null) yield return (node, simplified);

                    break;
                }

                case Neg neg:
                {
                    var simplified = TrySimplifyNeg(egraph, neg);
                    if (simplified is not null) yield return (node, simplified);

                    break;
                }
            }
    }

    private static AlgebraNode? TrySimplifyPhi(EGraph<AlgebraNode> egraph, Phi phi)
    {
        var phiValues = GetPhiValuesFromList(egraph, phi.values);
        if (phiValues.Length == 0) return new Literal<object?>(null);

        if (phiValues.Length == 1) return new StrategyNode.Extension("identity", [phiValues[0]]);

        var firstRoot = egraph.union_find.find(phiValues[0]);
        var allSame = true;

        for (var i = 1; i < phiValues.Length; i++)
            if (egraph.union_find.find(phiValues[i]) != firstRoot)
            {
                allSame = false;
                break;
            }

        if (allSame) return new StrategyNode.Extension("identity", [phiValues[0]]);

        return null;
    }

    /// <summary>
    ///     从 E-Graph 中提取 Phi 节点包装的值列表
    /// </summary>
    private static ImmutableArray<Id> GetPhiValuesFromList(EGraph<AlgebraNode> egraph, IReadOnlyList<Id> valuesList)
    {
        var result = ImmutableArray<Id>.Empty.ToBuilder();

        foreach (var valuesId in valuesList)
        {
            var eclass = egraph.get_class(valuesId);
            if (eclass is null) continue;

            foreach (var node in eclass.nodes)
                if (node is ArrayLit arr)
                    result.AddRange(arr.elements);
                else if (node is Tuple tuple)
                    result.AddRange(GetPhiValuesFromList(egraph, tuple.elements));
                else
                    result.Add(valuesId);
        }

        return result.ToImmutable();
    }

    private static AlgebraNode? TrySimplifyStore(EGraph<AlgebraNode> egraph, Store store)
    {
        var valueClass = egraph.get_class(store.value);
        if (valueClass is null) return null;

        foreach (var valueNode in valueClass.nodes)
            if (valueNode is Load load && AreSameRoot(egraph, load.pointer, store.pointer))
                return new Literal<object?>(null);

        return null;
    }

    private static AlgebraNode? TrySimplifyAdd(EGraph<AlgebraNode> egraph, Add add)
    {
        if (AreSameRoot(egraph, add.left, add.right))
        {
            var mulId = egraph.add(new Literal<long>(2)).value;
            return new Mul(add.left, mulId);
        }

        return null;
    }

    private static AlgebraNode? TrySimplifyMul(EGraph<AlgebraNode> egraph, Mul mul)
    {
        if (IsMinusOne(egraph, mul.right)) return new Neg(mul.left);

        if (IsMinusOne(egraph, mul.left)) return new Neg(mul.right);

        return null;
    }

    private static AlgebraNode? TrySimplifyNeg(EGraph<AlgebraNode> egraph, Neg neg)
    {
        var innerClass = egraph.get_class(neg.operand);
        if (innerClass is null) return null;

        foreach (var innerNode in innerClass.nodes)
            if (innerNode is Literal<long> c)
                try
                {
                    return new Literal<long>(checked(-c.value));
                }
                catch (OverflowException)
                {
                    return null;
                }

        return null;
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }

    private static bool IsMinusOne(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.Any(n => n is Literal<long> { value: -1 }) ?? false;
    }
}