namespace Core.Math.Tensor;

/// <summary>
///     计算节点接口，表示计算图中的一个节点。
/// </summary>
public interface IComputeNode
{
    /// <summary>
    ///     获取计算节点的名称。
    /// </summary>
    string name { get; }
}