using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.PartialEvaluate;

/// <summary>
///     PE（部分求值）工厂接口，用于方言降级。
///     将源方言的代数表达式降级为目标方言的代数表达式。
///     每个 PE 工厂实现对应一个方言降级对（如 Standard→Core）。
/// </summary>
public interface IPartialEvaluateFactory
{
    /// <summary>
    ///     PE 工厂名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     源方言名称
    /// </summary>
    string source_dialect { get; }

    /// <summary>
    ///     目标方言名称
    /// </summary>
    string target_dialect { get; }

    /// <summary>
    ///     对 EGraph 应用部分求值降级，将源方言节点转换为目标方言节点
    /// </summary>
    /// <param name="egraph">目标 EGraph</param>
    /// <returns>是否产生了新的等价关系</returns>
    bool apply(EGraph<AlgebraNode> egraph);
}