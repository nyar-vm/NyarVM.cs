using Nyar.IR.Intent;

namespace Nyar.IR.Rewrite;

/// <summary>
///     模式变量的扩展方法，用于在 [RewriteRule] 模式方法体中构建二元操作表达式。
///     这些方法由 Source Generator 分析，生成对应的 Oa 节点模式匹配和替换代码。
/// </summary>
public static class VarExtensions
{
    /// <summary>
    ///     加法模式变量
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>加法模式。</returns>
    public static AlgebraNode add(this object left, object right)
    {
        return default!;
    }

    /// <summary>
    ///     减法模式变量
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>减法模式。</returns>
    public static AlgebraNode sub(this object left, object right)
    {
        return default!;
    }

    /// <summary>
    ///     乘法模式变量
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>乘法模式。</returns>
    public static AlgebraNode mul(this object left, object right)
    {
        return default!;
    }

    /// <summary>
    ///     除法模式变量
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>除法模式。</returns>
    public static AlgebraNode div(this object left, object right)
    {
        return default!;
    }
}