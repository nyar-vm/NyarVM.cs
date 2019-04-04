using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core;

/// <summary>
///     Core 代数等价规则，使用 [RewriteRule] 属性模式定义。
///     这些规则由 Source Generator 自动生成 IRewriteRule&lt;Oa&gt; 实现类。
/// </summary>
public static class AlgebraicRules
{
    /// <summary>
    ///     获取所有重写规则（SG 生成的规则编译未就绪，返回空列表作为安全回退）
    /// </summary>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> generated_rules()
    {
        return [];
    }

    #region 加法规则

    /// <summary>
    ///     加法交换律：x.Add(y) == y.Add(x)
    /// </summary>
    [RewriteRule(name = "core.add-commute")]
    public static AlgebraNode add_commutative_pattern(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x, Var<AlgebraNode> y)
    {
        return x.add(y);
    }

    /// <summary>
    ///     加法交换律替换
    /// </summary>
    [RewriteReplacement(pattern = "core.add-commute")]
    public static AlgebraNode add_commutative_replacement(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x,
        Var<AlgebraNode> y)
    {
        return y.add(x);
    }

    /// <summary>
    ///     加法结合律：(x.Add(y)).Add(z) == x.Add(y.Add(z))
    /// </summary>
    [RewriteRule(name = "core.add-assoc")]
    public static AlgebraNode add_associative_pattern(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x, Var<AlgebraNode> y,
        Var<AlgebraNode> z)
    {
        return x.add(y).add(z);
    }

    /// <summary>
    ///     加法结合律替换
    /// </summary>
    [RewriteReplacement(pattern = "core.add-assoc")]
    public static AlgebraNode add_associative_replacement(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x,
        Var<AlgebraNode> y, Var<AlgebraNode> z)
    {
        return x.add(y.add(z));
    }

    #endregion

    #region 乘法规则

    /// <summary>
    ///     乘法交换律：x.Mul(y) == y.Mul(x)
    /// </summary>
    [RewriteRule(name = "core.mul-commute")]
    public static AlgebraNode mul_commutative_pattern(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x, Var<AlgebraNode> y)
    {
        return x.mul(y);
    }

    /// <summary>
    ///     乘法交换律替换
    /// </summary>
    [RewriteReplacement(pattern = "core.mul-commute")]
    public static AlgebraNode mul_commutative_replacement(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x,
        Var<AlgebraNode> y)
    {
        return y.mul(x);
    }

    /// <summary>
    ///     乘法结合律：(x.Mul(y)).Mul(z) == x.Mul(y.Mul(z))
    /// </summary>
    [RewriteRule(name = "core.mul-assoc")]
    public static AlgebraNode mul_associative_pattern(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x, Var<AlgebraNode> y,
        Var<AlgebraNode> z)
    {
        return x.mul(y).mul(z);
    }

    /// <summary>
    ///     乘法结合律替换
    /// </summary>
    [RewriteReplacement(pattern = "core.mul-assoc")]
    public static AlgebraNode mul_associative_replacement(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x,
        Var<AlgebraNode> y, Var<AlgebraNode> z)
    {
        return x.mul(y.mul(z));
    }

    /// <summary>
    ///     乘法对加法的分配律：x.Mul(y.Add(z)) == x.Mul(y).Add(x.Mul(z))
    /// </summary>
    [RewriteRule(name = "core.mul-distribute")]
    public static AlgebraNode mul_distributive_pattern(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x, Var<AlgebraNode> y,
        Var<AlgebraNode> z)
    {
        return x.mul(y.add(z));
    }

    /// <summary>
    ///     乘法对加法的分配律替换
    /// </summary>
    [RewriteReplacement(pattern = "core.mul-distribute")]
    public static AlgebraNode mul_distributive_replacement(EGraph<AlgebraNode> egraph, Var<AlgebraNode> x,
        Var<AlgebraNode> y, Var<AlgebraNode> z)
    {
        return x.mul(y).add(x.mul(z));
    }

    #endregion
}