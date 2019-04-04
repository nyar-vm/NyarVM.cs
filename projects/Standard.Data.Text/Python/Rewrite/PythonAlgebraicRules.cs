namespace Std.Data.Text.Python.Rewrite;

/// <summary>
///     Python 方言的代数重写规则集。
///     运算符语法已在进入 IR 时解糖为普通调用，因此此处不再基于通用运算节点做代数重写。
/// </summary>
public static class PythonAlgebraicRules
{
    /// <summary>
    ///     获取所有 Python 代数重写规则。
    /// </summary>
    public static IReadOnlyList<IRewriteRule<IKun>> AllRules => [];
}
