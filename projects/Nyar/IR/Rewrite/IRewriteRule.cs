using Nyar.EGraph;

namespace Nyar.IR.Rewrite;

/// <summary>
///     重写规则接口，定义 E-Graph 中的等价变换规则
/// </summary>
public interface IRewriteRule<T> where T : ILanguage<T>
{
    /// <summary>
    ///     规则名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     对 E-Graph 中的每个等价类应用此规则
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <returns>是否产生了新的等价关系。</returns>
    bool apply(EGraph<T> egraph);
}