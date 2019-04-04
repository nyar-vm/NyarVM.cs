using Nyar.IR.Intent;

namespace Nyar.EGraph;

/// <summary>
///     语言节点接口，定义 E-Graph 中节点类型的必要操作
/// </summary>
/// <typeparam name="T">实现此接口的语言节点类型</typeparam>
public interface ILanguage<out T> where T : ILanguage<T>
{
    /// <summary>
    ///     获取节点的所有子节点标识符
    /// </summary>
    /// <returns>子节点标识符列表。</returns>
    IReadOnlyList<Id> child_ids();

    /// <summary>
    ///     对节点的所有子节点标识符应用映射函数，返回新节点
    /// </summary>
    /// <param name="f">映射函数。</param>
    /// <returns>子节点被映射后的新节点。</returns>
    T map_children(Func<Id, Id> f);
}