namespace Std.Data.Text.C.Rewrite;

/// <summary>
///     C 方言的代数重写规则集。
///     当前 IR 已不再依赖通用运算节点，因此这里不再保留基于运算符字符串的代数重写。
/// </summary>
public static class CAlgebraicRules
{
    /// <summary>
    ///     获取所有 C 代数重写规则。
    /// </summary>
    public static IReadOnlyList<IRewriteRule<IKun>> AllRules => [];
}
