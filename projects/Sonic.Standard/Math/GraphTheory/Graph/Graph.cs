using System;
using System.Collections.Generic;
using System.Linq;

namespace Std.Math.GraphTheory.Graph;

/// <summary>
///     有向图抽象基类，提供基于邻接表的默认存储实现。
///     子类只需关注领域特定逻辑，无需重复实现图的基础结构。
/// </summary>
/// <typeparam name="TNode">节点类型</typeparam>
/// <typeparam name="TEdge">边类型，推荐实现 <see cref="IEdge{TNode}"/> 以便使用图算法</typeparam>
public abstract class Graph<TNode, TEdge> : IGraph<TNode, TEdge>
{
    #region 受保护字段

    /// <summary>
    ///     出边映射：源节点 → 从该节点出发的边列表。
    /// </summary>
    protected readonly Dictionary<TNode, List<TEdge>> _successors;

    /// <summary>
    ///     入边映射：目标节点 → 指向该节点的边列表。
    /// </summary>
    protected readonly Dictionary<TNode, List<TEdge>> _predecessors;

    /// <summary>
    ///     节点集合，用于快速存在性检查。
    /// </summary>
    protected readonly HashSet<TNode> _nodeSet;

    /// <summary>
    ///     边列表，维护插入顺序。
    /// </summary>
    protected readonly List<TEdge> _edgeList;

    /// <summary>
    ///     边集合，用于快速去重。
    /// </summary>
    protected readonly HashSet<TEdge> _edgeSet;

    /// <summary>
    ///     节点相等比较器。
    /// </summary>
    protected readonly IEqualityComparer<TNode> _nodeComparer;

    /// <summary>
    ///     边相等比较器。
    /// </summary>
    protected readonly IEqualityComparer<TEdge> _edgeComparer;

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化图，使用默认相等比较器。
    /// </summary>
    protected Graph()
        : this(EqualityComparer<TNode>.Default, EqualityComparer<TEdge>.Default)
    {
    }

    /// <summary>
    ///     初始化图，使用自定义相等比较器。
    /// </summary>
    /// <param name="nodeComparer">节点相等比较器</param>
    /// <param name="edgeComparer">边相等比较器</param>
    protected Graph(IEqualityComparer<TNode> nodeComparer, IEqualityComparer<TEdge> edgeComparer)
    {
        _nodeComparer = nodeComparer ?? EqualityComparer<TNode>.Default;
        _edgeComparer = edgeComparer ?? EqualityComparer<TEdge>.Default;
        _successors = new Dictionary<TNode, List<TEdge>>(_nodeComparer);
        _predecessors = new Dictionary<TNode, List<TEdge>>(_nodeComparer);
        _nodeSet = new HashSet<TNode>(_nodeComparer);
        _edgeList = [];
        _edgeSet = new HashSet<TEdge>(_edgeComparer);
    }

    #endregion

    #region IGraph 实现

    /// <inheritdoc />
    public int NodeCount => _nodeSet.Count;

    /// <inheritdoc />
    public int EdgeCount => _edgeSet.Count;

    /// <inheritdoc />
    public IReadOnlyList<TNode> Nodes => [.. _nodeSet];

    /// <inheritdoc />
    public IReadOnlyList<TEdge> Edges => _edgeList.AsReadOnly();

    /// <inheritdoc />
    public virtual void AddNode(TNode node)
    {
        if (_nodeSet.Add(node))
        {
            _successors[node] = [];
            _predecessors[node] = [];
        }
    }

    /// <inheritdoc />
    public virtual bool RemoveNode(TNode node)
    {
        if (!_nodeSet.Remove(node))
        {
            return false;
        }

        // 收集所有与该节点关联的边
        var incidentEdges = new HashSet<TEdge>(_edgeComparer);

        if (_successors.TryGetValue(node, out var outEdges))
        {
            foreach (var edge in outEdges)
            {
                incidentEdges.Add(edge);
            }
            _successors.Remove(node);
        }

        if (_predecessors.TryGetValue(node, out var inEdges))
        {
            foreach (var edge in inEdges)
            {
                incidentEdges.Add(edge);
            }
            _predecessors.Remove(node);
        }

        // 从全局边集合中移除
        foreach (var edge in incidentEdges)
        {
            _edgeSet.Remove(edge);
            _edgeList.Remove(edge);
        }

        // 从其他节点的邻接表中清除这些边
        foreach (var list in _successors.Values)
        {
            list.RemoveAll(e => incidentEdges.Contains(e));
        }
        foreach (var list in _predecessors.Values)
        {
            list.RemoveAll(e => incidentEdges.Contains(e));
        }

        return true;
    }

    /// <inheritdoc />
    public virtual void AddEdge(TEdge edge)
    {
        if (!_edgeSet.Add(edge))
        {
            return;
        }

        _edgeList.Add(edge);

        var source = GetEdgeSource(edge);
        var target = GetEdgeTarget(edge);

        AddNode(source);
        AddNode(target);

        _successors[source].Add(edge);
        _predecessors[target].Add(edge);
    }

    /// <inheritdoc />
    public virtual bool RemoveEdge(TEdge edge)
    {
        if (!_edgeSet.Remove(edge))
        {
            return false;
        }

        _edgeList.Remove(edge);

        var source = GetEdgeSource(edge);
        var target = GetEdgeTarget(edge);

        if (_successors.TryGetValue(source, out var outEdges))
        {
            outEdges.Remove(edge);
        }

        if (_predecessors.TryGetValue(target, out var inEdges))
        {
            inEdges.Remove(edge);
        }

        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<TNode> GetSuccessors(TNode node)
    {
        if (!_successors.TryGetValue(node, out var edges))
        {
            return [];
        }

        var result = new List<TNode>(edges.Count);
        foreach (var edge in edges)
        {
            result.Add(GetEdgeTarget(edge));
        }
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<TNode> GetPredecessors(TNode node)
    {
        if (!_predecessors.TryGetValue(node, out var edges))
        {
            return [];
        }

        var result = new List<TNode>(edges.Count);
        foreach (var edge in edges)
        {
            result.Add(GetEdgeSource(edge));
        }
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<TEdge> GetOutEdges(TNode node)
    {
        return _successors.TryGetValue(node, out var edges)
            ? edges.AsReadOnly()
            : Array.Empty<TEdge>();
    }

    /// <inheritdoc />
    public IReadOnlyList<TEdge> GetInEdges(TNode node)
    {
        return _predecessors.TryGetValue(node, out var edges)
            ? edges.AsReadOnly()
            : Array.Empty<TEdge>();
    }

    /// <inheritdoc />
    public bool ContainsNode(TNode node)
    {
        return _nodeSet.Contains(node);
    }

    /// <inheritdoc />
    public bool ContainsEdge(TEdge edge)
    {
        return _edgeSet.Contains(edge);
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     清空图中所有的节点和边。
    /// </summary>
    public virtual void Clear()
    {
        _nodeSet.Clear();
        _edgeSet.Clear();
        _edgeList.Clear();
        _successors.Clear();
        _predecessors.Clear();
    }

    #endregion

    #region 抽象方法（子类实现以提供边属性提取）

    /// <summary>
    ///     从边中提取源节点。子类必须实现此方法。
    /// </summary>
    /// <param name="edge">有向边</param>
    /// <returns>源节点</returns>
    protected abstract TNode GetEdgeSource(TEdge edge);

    /// <summary>
    ///     从边中提取目标节点。子类必须实现此方法。
    /// </summary>
    /// <param name="edge">有向边</param>
    /// <returns>目标节点</returns>
    protected abstract TNode GetEdgeTarget(TEdge edge);

    #endregion
}