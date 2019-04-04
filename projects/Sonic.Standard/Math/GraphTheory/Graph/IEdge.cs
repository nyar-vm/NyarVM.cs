namespace Std.Math.GraphTheory.Graph;

/// <summary>
///     有向边接口，提供源节点和目标节点的只读访问。
/// </summary>
/// <typeparam name="TNode">节点类型</typeparam>
public interface IEdge<out TNode>
{
    /// <summary>
    ///     获取边的源节点。
    /// </summary>
    TNode Source { get; }

    /// <summary>
    ///     获取边的目标节点。
    /// </summary>
    TNode Target { get; }
}