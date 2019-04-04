using Nyar.IR.Intent;

namespace Nyar.EGraph;

/// <summary>
///     并查集数据结构，支持路径压缩和按秩合并的等价类查找与合并
/// </summary>
public class UnionFind
{
    private uint[] _parents;
    private int[] _ranks;

    /// <summary>
    ///     创建空的并查集
    /// </summary>
    public UnionFind() : this(64)
    {
    }

    /// <summary>
    ///     创建指定初始容量的并查集
    /// </summary>
    /// <param name="initialCapacity">初始容量。</param>
    public UnionFind(int initialCapacity)
    {
        _parents = new uint[initialCapacity];
        _ranks = new int[initialCapacity];
        count = 0;
    }

    /// <summary>
    ///     当前元素数量
    /// </summary>
    public int count { get; private set; }

    /// <summary>
    ///     确保容量足够容纳指定 id
    /// </summary>
    private void ensure_capacity(uint id)
    {
        if (id < _parents.Length) return;

        var newCapacity = _parents.Length;
        while (newCapacity <= id) newCapacity *= 2;

        Array.Resize(ref _parents, newCapacity);
        Array.Resize(ref _ranks, newCapacity);
    }

    /// <summary>
    ///     注册一个新的等价类标识符
    /// </summary>
    /// <param name="id">要注册的标识符。</param>
    public void register(Id id)
    {
        ensure_capacity(id.value);
        if (id.value >= count)
        {
            for (var i = count; i <= id.value; i++)
            {
                _parents[i] = (uint)i;
                _ranks[i] = 0;
            }

            count = (int)(id.value + 1);
        }
    }

    /// <summary>
    ///     查找标识符的根节点，同时进行路径压缩
    /// </summary>
    /// <param name="id">待查找的标识符。</param>
    /// <returns>根节点标识符。</returns>
    public Id find(Id id)
    {
        if (id.value >= count) return id;

        var root = id.value;
        while (_parents[root] != root) root = _parents[root];

        var curr = id.value;
        while (_parents[curr] != root)
        {
            var next = _parents[curr];
            _parents[curr] = root;
            curr = next;
        }

        return new Id(root);
    }

    /// <summary>
    ///     合并两个标识符所在的等价类（按秩合并）
    /// </summary>
    /// <param name="id1">第一个标识符。</param>
    /// <param name="id2">第二个标识符。</param>
    /// <returns>合并后的根节点标识符。</returns>
    public Id union(Id id1, Id id2)
    {
        var root1 = find(id1);
        var root2 = find(id2);
        if (root1 == root2) return root1;

        var rank1 = _ranks[root1.value];
        var rank2 = _ranks[root2.value];

        if (rank1 < rank2)
        {
            _parents[root1.value] = root2.value;
            return root2;
        }

        if (rank1 > rank2)
        {
            _parents[root2.value] = root1.value;
            return root1;
        }

        _parents[root1.value] = root2.value;
        _ranks[root2.value] = rank2 + 1;
        return root2;
    }
}