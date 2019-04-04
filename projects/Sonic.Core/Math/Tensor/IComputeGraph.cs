namespace Core.Math.Tensor;

/// <summary>
///     计算图接口，定义计算节点的添加和连接方式。
/// </summary>
public interface IComputeGraph
{
    /// <summary>
    ///     向计算图中添加一个具有指定名称的计算节点。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <returns>新添加的计算节点。</returns>
    IComputeNode add_node(string name);

    /// <summary>
    ///     在两个计算节点之间建立连接。
    /// </summary>
    /// <param name="from">起始节点。</param>
    /// <param name="to">目标节点。</param>
    void connect(IComputeNode from, IComputeNode to);
}