using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     恒等规则集合
/// </summary>
public static class IdentityRules
{
    /// <summary>
    ///     加法零恒等：Add(a, 0) → a
    /// </summary>
    [RewriteRule(name = "add-zero-identity")]
    public static AlgebraNode? AddZeroIdentity(EGraph<AlgebraNode> egraph, AlgebraNode node)
    {
        if (node is not Add add) return null;

        if (IsZero(egraph, add.right)) return new StrategyNode.Extension("identity", [add.left]);

        if (IsZero(egraph, add.left)) return new StrategyNode.Extension("identity", [add.right]);

        return null;
    }

    /// <summary>
    ///     乘法一恒等：Mul(a, 1) → a
    /// </summary>
    [RewriteRule(name = "mul-one-identity")]
    public static AlgebraNode? MulOneIdentity(EGraph<AlgebraNode> egraph, AlgebraNode node)
    {
        if (node is not Mul mul) return null;

        if (IsOne(egraph, mul.right)) return new StrategyNode.Extension("identity", [mul.left]);

        if (IsOne(egraph, mul.left)) return new StrategyNode.Extension("identity", [mul.right]);

        return null;
    }

    /// <summary>
    ///     乘法零吸收：Mul(a, 0) → 0
    /// </summary>
    [RewriteRule(name = "mul-zero-annihilator")]
    public static AlgebraNode? MulZeroAnnihilator(EGraph<AlgebraNode> egraph, AlgebraNode node)
    {
        if (node is not Mul mul) return null;

        if (IsZero(egraph, mul.right) || IsZero(egraph, mul.left)) return new Literal<long>(0);

        return null;
    }

    private static bool IsZero(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.Any(n => n is Literal<long> { value: 0 }) ?? false;
    }

    private static bool IsOne(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.Any(n => n is Literal<long> { value: 1 }) ?? false;
    }
}