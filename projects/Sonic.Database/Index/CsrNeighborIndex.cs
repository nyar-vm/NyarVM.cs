namespace Olympus.Athena.Index;

#region CsrNeighborIndex CSR 图邻居索引

/// <summary>
///     基于 CSR（Compressed Sparse Row，压缩稀疏行）格式的图邻居索引，
///     支持按边类型管理有向图，提供高效的出边和入边遍历
/// </summary>
public sealed class CsrNeighborIndex : IGraphIndex
{
    #region 构造函数

    /// <summary>
    ///     创建空的 CSR 图邻居索引
    /// </summary>
    public CsrNeighborIndex()
    {
        _rawEdges = new Dictionary<string, List<(long from, long to)>>();
        _edgeSets = new Dictionary<string, HashSet<(long from, long to)>>();

        _forwardCsr = new Dictionary<string, (long[] offsets, long[] adjacency)>();
        _forwardVertexMap = new Dictionary<string, Dictionary<long, int>>();

        _backwardCsr = new Dictionary<string, (long[] offsets, long[] adjacency)>();
        _backwardVertexMap = new Dictionary<string, Dictionary<long, int>>();

        _dirtyForward = [];
        _dirtyBackward = [];
    }

    #endregion

    #region 添加边

    /// <summary>
    ///     添加一条有向边
    /// </summary>
    /// <param name="from">起始顶点 ID</param>
    /// <param name="to">目标顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    public void AddEdge(long from, long to, string edgeType)
    {
        EnsureEdgeTypeExists(edgeType);

        var edge = (from, to);
        if (_edgeSets[edgeType].Add(edge))
        {
            _rawEdges[edgeType].Add(edge);
            _dirtyForward.Add(edgeType);
            _dirtyBackward.Add(edgeType);
        }
    }

    #endregion

    #region 判断边是否存在

    /// <summary>
    ///     判断指定边是否存在
    /// </summary>
    /// <param name="from">起始顶点 ID</param>
    /// <param name="to">目标顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    /// <returns>边存在时返回 <c>true</c></returns>
    public bool HasEdge(long from, long to, string edgeType)
    {
        if (!_edgeSets.TryGetValue(edgeType, out var set)) return false;

        return set.Contains((from, to));
    }

    #endregion

    #region 移除边

    /// <summary>
    ///     移除一条有向边
    /// </summary>
    /// <param name="from">起始顶点 ID</param>
    /// <param name="to">目标顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    public void RemoveEdge(long from, long to, string edgeType)
    {
        if (!_edgeSets.TryGetValue(edgeType, out var set)) return;

        var edge = (from, to);
        if (!set.Remove(edge)) return;

        _rawEdges[edgeType].Remove(edge);
        _dirtyForward.Add(edgeType);
        _dirtyBackward.Add(edgeType);
    }

    #endregion

    #region 字段

    private readonly Dictionary<string, List<(long from, long to)>> _rawEdges;
    private readonly Dictionary<string, HashSet<(long from, long to)>> _edgeSets;

    private readonly Dictionary<string, (long[] offsets, long[] adjacency)> _forwardCsr;
    private readonly Dictionary<string, Dictionary<long, int>> _forwardVertexMap;

    private readonly Dictionary<string, (long[] offsets, long[] adjacency)> _backwardCsr;
    private readonly Dictionary<string, Dictionary<long, int>> _backwardVertexMap;

    private readonly HashSet<string> _dirtyForward;
    private readonly HashSet<string> _dirtyBackward;

    #endregion

    #region 获取邻居

    /// <summary>
    ///     获取指定顶点的出边邻居列表
    /// </summary>
    /// <param name="vertexId">顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    /// <returns>邻居顶点 ID 的只读列表</returns>
    public IReadOnlyList<long> GetNeighbors(long vertexId, string edgeType)
    {
        RebuildForwardIfDirty(edgeType);

        if (!_forwardVertexMap.TryGetValue(edgeType, out var map)) return [];

        if (!map.TryGetValue(vertexId, out var idx)) return [];

        var (offsets, adjacency) = _forwardCsr[edgeType];
        var start = offsets[idx];
        var end = offsets[idx + 1];
        var count = (int)(end - start);
        var result = new long[count];
        Array.Copy(adjacency, start, result, 0, count);
        return result;
    }

    /// <summary>
    ///     获取指定顶点的入边邻居列表
    /// </summary>
    /// <param name="vertexId">顶点 ID</param>
    /// <param name="edgeType">边类型</param>
    /// <returns>入边邻居顶点 ID 的只读列表</returns>
    public IReadOnlyList<long> GetIncoming(long vertexId, string edgeType)
    {
        RebuildBackwardIfDirty(edgeType);

        if (!_backwardVertexMap.TryGetValue(edgeType, out var map)) return [];

        if (!map.TryGetValue(vertexId, out var idx)) return [];

        var (offsets, adjacency) = _backwardCsr[edgeType];
        var start = offsets[idx];
        var end = offsets[idx + 1];
        var count = (int)(end - start);
        var result = new long[count];
        Array.Copy(adjacency, start, result, 0, count);
        return result;
    }

    #endregion

    #region 内部方法

    private void EnsureEdgeTypeExists(string edgeType)
    {
        if (!_rawEdges.ContainsKey(edgeType))
        {
            _rawEdges[edgeType] = [];
            _edgeSets[edgeType] = [];
        }
    }

    private void RebuildForwardIfDirty(string edgeType)
    {
        if (!_dirtyForward.Contains(edgeType)) return;

        _dirtyForward.Remove(edgeType);

        if (!_rawEdges.TryGetValue(edgeType, out var edges) || edges.Count == 0)
        {
            _forwardCsr.Remove(edgeType);
            _forwardVertexMap.Remove(edgeType);
            return;
        }

        var vertexSet = new SortedSet<long>();
        foreach (var (from, to) in edges) vertexSet.Add(from);

        var vertexList = new List<long>(vertexSet);
        var vertexMap = new Dictionary<long, int>();
        for (var i = 0; i < vertexList.Count; i++) vertexMap[vertexList[i]] = i;

        var adjacency = new Dictionary<int, List<long>>();
        for (var i = 0; i < vertexList.Count; i++) adjacency[i] = [];

        foreach (var (from, to) in edges)
        {
            var idx = vertexMap[from];
            adjacency[idx].Add(to);
        }

        var adjacencyFlat = new List<long>();
        var offsets = new long[vertexList.Count + 1];
        for (var i = 0; i < vertexList.Count; i++)
        {
            offsets[i] = adjacencyFlat.Count;
            adjacency[i].Sort();
            adjacencyFlat.AddRange(adjacency[i]);
        }

        offsets[vertexList.Count] = adjacencyFlat.Count;

        _forwardCsr[edgeType] = (offsets, [.. adjacencyFlat]);
        _forwardVertexMap[edgeType] = vertexMap;
    }

    private void RebuildBackwardIfDirty(string edgeType)
    {
        if (!_dirtyBackward.Contains(edgeType)) return;

        _dirtyBackward.Remove(edgeType);

        if (!_rawEdges.TryGetValue(edgeType, out var edges) || edges.Count == 0)
        {
            _backwardCsr.Remove(edgeType);
            _backwardVertexMap.Remove(edgeType);
            return;
        }

        var vertexSet = new SortedSet<long>();
        foreach (var (from, to) in edges) vertexSet.Add(to);

        var vertexList = new List<long>(vertexSet);
        var vertexMap = new Dictionary<long, int>();
        for (var i = 0; i < vertexList.Count; i++) vertexMap[vertexList[i]] = i;

        var adjacency = new Dictionary<int, List<long>>();
        for (var i = 0; i < vertexList.Count; i++) adjacency[i] = [];

        foreach (var (from, to) in edges)
        {
            var idx = vertexMap[to];
            adjacency[idx].Add(from);
        }

        var adjacencyFlat = new List<long>();
        var offsets = new long[vertexList.Count + 1];
        for (var i = 0; i < vertexList.Count; i++)
        {
            offsets[i] = adjacencyFlat.Count;
            adjacency[i].Sort();
            adjacencyFlat.AddRange(adjacency[i]);
        }

        offsets[vertexList.Count] = adjacencyFlat.Count;

        _backwardCsr[edgeType] = (offsets, [.. adjacencyFlat]);
        _backwardVertexMap[edgeType] = vertexMap;
    }

    #endregion
}

#endregion