namespace Std.Math.GraphTheory.Graph;

/// <summary>
///     默认有向边实现，包含源节点、目标节点和可选权重。
/// </summary>
/// <typeparam name="TNode">节点类型</typeparam>
/// <param name="Source">源节点</param>
/// <param name="Target">目标节点</param>
/// <param name="Weight">边权重，默认为 <c>1.0</c></param>
public sealed record Edge<TNode>(TNode Source, TNode Target, double Weight = 1.0) : IEdge<TNode>;