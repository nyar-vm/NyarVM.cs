using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core;

/// <summary>
///     Core 方言的 OA 风格重写规则注册。
///     将现有规则按类别组织，提供统一的工厂方法。
///     后续将逐步迁移为 OARuleEngine 强类型声明。
/// </summary>
public static class OaCoreRules
{
    /// <summary>
    ///     获取所有 Core 代数等价规则
    /// </summary>
    /// <returns>代数等价规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> algebraic_rules()
    {
        return Core.AlgebraicRules.generated_rules();
    }

    // OA 重构中：以下规则类文件已排除编译，暂时禁用
    /// <summary>
    ///     获取所有 Core 常量折叠规则
    /// </summary>
    /// <returns>常量折叠规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> constant_folding_rules()
    {
        return
        [
            // new ConstantFoldingRule(),
            // new SubZeroRule(),
            // new SubSelfRule(),
            // new DivSelfRule(),
            // new DivNegRule(),
            // new DivDistributiveRule(),
            // new RemSelfRule(),
            // new MulIdentityRule()
        ];
    }

    /// <summary>
    ///     获取所有 Core 死代码消除规则
    /// </summary>
    /// <returns>死代码消除规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> dead_code_elimination_rules()
    {
        return
        [
            // new DeadCodeEliminationRule(),
            // new CommonSubexpressionEliminationRule(),
            // new CopyPropagationRule()
        ];
    }

    /// <summary>
    ///     获取所有 Core 布尔简化规则
    /// </summary>
    /// <returns>布尔简化规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> boolean_simplification_rules()
    {
        return
        [
            // new BooleanSimplificationRule(),
            // new BooleanIdentityRule(),
            // new DeMorganRule()
        ];
    }

    /// <summary>
    ///     获取所有 Core 否定规则
    /// </summary>
    /// <returns>否定规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> negation_rules()
    {
        return
        [
            // new NegNegEliminationRule(),
            // new NegDistributiveRule(),
            // new NegSubRule(),
            // new MulNegRule(),
            // new MulNegNegRule(),
            // new SubToNegAddRule(),
            // new AddNegToSubRule()
        ];
    }

    /// <summary>
    ///     获取所有 Core 比较规则
    /// </summary>
    /// <returns>比较规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> comparison_rules()
    {
        return
        [
            // new CmpSelfRule(),
            // new CmpSymmetryRule(),
            // new CmpNegateRule(),
            // new CmpWithZeroRule(),
            // new CmpConstantRule()
        ];
    }

    /// <summary>
    ///     获取所有 Core 重写规则
    /// </summary>
    /// <returns>所有重写规则列表。</returns>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> all_rules()
    {
        var rules = new List<IRewriteRule<AlgebraNode>>();
        rules.AddRange(algebraic_rules());
        rules.AddRange(constant_folding_rules());
        rules.AddRange(dead_code_elimination_rules());
        rules.AddRange(boolean_simplification_rules());
        rules.AddRange(negation_rules());
        rules.AddRange(comparison_rules());
        return rules;
    }
}