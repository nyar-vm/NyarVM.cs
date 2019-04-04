using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Standard;

/// <summary>
///     `OA` 时代的 Standard 规则入口兼容层。
///     旧代码仍引用该文件名或类型名时，统一转发到 `StandardRules`。
/// </summary>
public static class OAStandardRules
{
    /// <summary>
    ///     获取所有 Standard 重写规则。
    /// </summary>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> all_rewrite_rules()
    {
        return StandardRules.all_rewrite_rules();
    }

    /// <summary>
    ///     兼容旧的 PascalCase 入口名。
    /// </summary>
    public static IReadOnlyList<IRewriteRule<AlgebraNode>> AllRewriteRules()
    {
        return StandardRules.all_rewrite_rules();
    }
}
