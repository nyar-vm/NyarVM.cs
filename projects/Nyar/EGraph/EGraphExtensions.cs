using Nyar.IR.Intent;

namespace Nyar.EGraph;

/// <summary>
///     EGraph 扩展工具方法
/// </summary>
public static class EGraphExtensions
{
    /// <summary>
    ///     尝试从 EGraph 的 e-class 中提取常量值
    /// </summary>
    /// <typeparam name="T">常量类型</typeparam>
    /// <param name="egraph">EGraph 实例</param>
    /// <param name="id">节点 ID</param>
    /// <param name="value">提取到的常量值</param>
    /// <returns>是否成功提取常量</returns>
    public static bool TryGetConstant<T>(this EGraph<AlgebraNode> egraph, Id id, out T value)
    {
        value = default!;
        var eclass = egraph.get_class(id);
        if (eclass is null) return false;

        foreach (var node in eclass.nodes)
        {
            if (node is Literal<T> lit)
            {
                value = lit.value;
                return true;
            }
        }

        return false;
    }
}