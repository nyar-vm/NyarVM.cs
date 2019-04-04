namespace Core.Math.Graph;

/// <summary>
///     节点索引接口，提供图中节点的整数值索引。
/// </summary>
public interface INodeIndex
{
    /// <summary>
    ///     获取节点的整数值索引。
    /// </summary>
    int value { get; }
}