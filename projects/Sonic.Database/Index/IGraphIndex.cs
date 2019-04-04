namespace Olympus.Athena.Index;

/// <summary>
///     图遍历索引接口，支持按边类型管理有向图的邻接关系
/// </summary>
public interface IGraphIndex
{
    /// <summary>
    ///     添加一条有向边
    /// </summary>
    /// <param name="from">起始顶点 ID</param>
    /// <param name="to">目标顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    void AddEdge(long from, long to, string edgeType);

    /// <summary>
    ///     获取指定顶点的出边邻居列表
    /// </summary>
    /// <param name="vertexId">顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    /// <returns>邻居顶点 ID 的只读列表</returns>
    IReadOnlyList<long> GetNeighbors(long vertexId, string edgeType);

    /// <summary>
    ///     获取指定顶点的入边邻居列表
    /// </summary>
    /// <param name="vertexId">顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    /// <returns>入边邻居顶点 ID 的只读列表</returns>
    IReadOnlyList<long> GetIncoming(long vertexId, string edgeType);

    /// <summary>
    ///     判断指定边是否存在
    /// </summary>
    /// <param name="from">起始顶点 ID</param>
    /// <param name="to">目标顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    /// <returns>边存在时返回 <c>true</c></returns>
    bool HasEdge(long from, long to, string edgeType);

    /// <summary>
    ///     移除一条有向边
    /// </summary>
    /// <param name="from">起始顶点 ID</param>
    /// <param name="to">目标顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    void RemoveEdge(long from, long to, string edgeType);
}