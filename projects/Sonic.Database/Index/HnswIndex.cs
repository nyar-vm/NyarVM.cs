namespace Olympus.Athena.Index;

#region HnswIndex HNSW 向量索引

/// <summary>
///     基于 HNSW（Hierarchical Navigable Small World）算法的向量近似最近邻搜索索引
/// </summary>
public sealed class HnswIndex : IVectorIndex
{
    #region 构造函数

    /// <summary>
    ///     创建 HNSW 向量索引
    /// </summary>
    /// <param name="dimension">向量维度</param>
    /// <param name="m">最大连接数，默认 16</param>
    /// <param name="efConstruction">构建时的搜索宽度，默认 200</param>
    /// <param name="efSearch">搜索时的搜索宽度，默认 50</param>
    public HnswIndex(int dimension, int m = 16, int efConstruction = 200, int efSearch = 50)
    {
        Dimension = dimension;
        _m = m;
        _efConstruction = efConstruction;
        _efSearch = efSearch;
        _ml = 1.0f / MathF.Log(m);
        Metric = DistanceMetric.L2;

        _layers = new Dictionary<int, Dictionary<long, List<long>>>();
        _vectors = new Dictionary<long, float[]>();
        _entryPoint = -1;
        _maxLevel = -1;
        _random = new Random();
    }

    #endregion

    #region 构建索引

    /// <summary>
    ///     批量构建向量索引，逐个插入向量并构建多层图结构
    /// </summary>
    /// <param name="vectors">向量集合</param>
    /// <param name="ids">与向量一一对应的标识符集合</param>
    public void Build(IReadOnlyList<float[]> vectors, IReadOnlyList<long> ids)
    {
        _layers.Clear();
        _vectors.Clear();
        _entryPoint = -1;
        _maxLevel = -1;

        for (var i = 0; i < vectors.Count; i++) Insert(vectors[i], ids[i]);
    }

    #endregion

    #region 搜索

    /// <summary>
    ///     搜索与查询向量最相似的 Top-K 个向量
    /// </summary>
    /// <param name="query">查询向量</param>
    /// <param name="k">返回的最相似向量数量</param>
    /// <returns>包含匹配标识符和对应距离的元组</returns>
    public (long[] ids, float[] distances) Search(float[] query, int k)
    {
        if (_entryPoint == -1 || _vectors.Count == 0) return ([], []);

        var ep = _entryPoint;
        var entryPoints = new List<long> { ep };

        for (var lc = _maxLevel; lc > 0; lc--)
        {
            var result = SearchLayer(query, entryPoints, 1, lc);
            entryPoints = [];
            foreach (var (id, _) in result) entryPoints.Add(id);
        }

        var final = SearchLayer(query, entryPoints, _efSearch, 0);
        final.Sort((a, b) => a.dist.CompareTo(b.dist));

        var count = Math.Min(k, final.Count);
        var ids = new long[count];
        var distances = new float[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = final[i].id;
            distances[i] = final[i].dist;
        }

        return (ids, distances);
    }

    #endregion

    #region 字段

    private readonly int _m;
    private readonly int _efConstruction;
    private readonly int _efSearch;
    private readonly float _ml;

    private readonly Dictionary<int, Dictionary<long, List<long>>> _layers;
    private readonly Dictionary<long, float[]> _vectors;
    private long _entryPoint;
    private int _maxLevel;
    private readonly Random _random;

    #endregion

    #region 属性

    /// <summary>
    ///     索引中的向量维度
    /// </summary>
    public int Dimension { get; }

    /// <summary>
    ///     当前使用的距离度量方式，默认 L2
    /// </summary>
    public DistanceMetric Metric { get; }

    #endregion

    #region 距离计算

    /// <summary>
    ///     计算两个向量之间的 L2 欧氏距离
    /// </summary>
    /// <param name="a">向量 a</param>
    /// <param name="b">向量 b</param>
    /// <returns>L2 欧氏距离</returns>
    public float L2Distance(float[] a, float[] b)
    {
        var sum = 0.0f;
        for (var i = 0; i < Dimension; i++)
        {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }

        return MathF.Sqrt(sum);
    }

    /// <summary>
    ///     计算两个向量之间的余弦距离（1 - 余弦相似度）
    /// </summary>
    /// <param name="a">向量 a</param>
    /// <param name="b">向量 b</param>
    /// <returns>余弦距离</returns>
    public float CosineDistance(float[] a, float[] b)
    {
        var dot = 0.0f;
        var normA = 0.0f;
        var normB = 0.0f;

        for (var i = 0; i < Dimension; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0.0f || normB == 0.0f) return 1.0f;

        return 1.0f - dot / MathF.Sqrt(normA * normB);
    }

    #endregion

    #region 内部方法

    private float CalculateDistance(float[] a, float[] b)
    {
        return Metric switch
        {
            DistanceMetric.L2 => L2Distance(a, b),
            DistanceMetric.Cosine => CosineDistance(a, b),
            _ => L2Distance(a, b)
        };
    }

    private int RandomLevel()
    {
        var r = _random.NextDouble();
        return (int)(-Math.Log(Math.Max(r, double.Epsilon)) * _ml);
    }

    private void Insert(float[] vector, long id)
    {
        _vectors[id] = vector;
        var level = RandomLevel();

        if (_entryPoint == -1)
        {
            _entryPoint = id;
            _maxLevel = level;
            for (var l = 0; l <= level; l++)
            {
                EnsureLayer(l);
                _layers[l][id] = [];
            }

            return;
        }

        var ep = _entryPoint;

        for (var lc = _maxLevel; lc > level; lc--)
        {
            var result = SearchLayer(vector, new List<long> { ep }, 1, lc);
            if (result.Count > 0) ep = result[0].id;
        }

        var entryPoints = new List<long> { ep };
        var minLevel = Math.Min(level, _maxLevel);
        for (var lc = minLevel; lc >= 0; lc--)
        {
            var w = SearchLayer(vector, entryPoints, _efConstruction, lc);
            var neighbors = SelectNeighbors(vector, w, _m);

            EnsureLayer(lc);
            _layers[lc][id] = neighbors;

            foreach (var n in neighbors)
            {
                if (!_layers[lc].ContainsKey(n)) _layers[lc][n] = [];

                _layers[lc][n].Add(id);
                if (_layers[lc][n].Count > _m)
                {
                    var nCandidates = new List<(long id, float dist)>();
                    foreach (var nn in _layers[lc][n])
                        nCandidates.Add((nn, CalculateDistance(_vectors[n], _vectors[nn])));

                    _layers[lc][n] = SelectNeighbors(_vectors[n], nCandidates, _m);
                }
            }

            entryPoints = [];
            foreach (var (wId, _) in w) entryPoints.Add(wId);
        }

        if (level > _maxLevel)
        {
            _maxLevel = level;
            _entryPoint = id;
        }
    }

    private List<(long id, float dist)> SearchLayer(float[] query, IReadOnlyList<long> entryPoints, int ef, int layer)
    {
        var visited = new HashSet<long>();
        var candidates = new PriorityQueue<long, float>();
        var results = new List<(long id, float dist)>();

        foreach (var ep in entryPoints)
        {
            var dist = CalculateDistance(query, _vectors[ep]);
            visited.Add(ep);
            candidates.Enqueue(ep, dist);
            results.Add((ep, dist));
        }

        results.Sort((a, b) => a.dist.CompareTo(b.dist));

        while (candidates.Count > 0)
        {
            candidates.TryDequeue(out var c, out var cDist);

            if (results.Count >= ef && cDist > results[results.Count - 1].dist) break;

            var neighbors = GetNeighbors(c, layer);
            foreach (var n in neighbors)
            {
                if (!visited.Add(n)) continue;

                if (!_vectors.TryGetValue(n, out var vector)) continue;

                var nDist = CalculateDistance(query, vector);

                if (results.Count >= ef && nDist >= results[results.Count - 1].dist) continue;

                candidates.Enqueue(n, nDist);
                results.Add((n, nDist));
                results.Sort((a, b) => a.dist.CompareTo(b.dist));

                if (results.Count > ef) results.RemoveAt(results.Count - 1);
            }
        }

        return results;
    }

    private List<long> GetNeighbors(long id, int layer)
    {
        if (!_layers.TryGetValue(layer, out var layer1)) return [];

        if (!layer1.ContainsKey(id)) return [];

        return _layers[layer][id];
    }

    private List<long> SelectNeighbors(float[] query, List<(long id, float dist)> candidates, int m)
    {
        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
        var result = new List<long>(m);
        var limit = Math.Min(m, candidates.Count);
        for (var i = 0; i < limit; i++) result.Add(candidates[i].id);

        return result;
    }

    private void EnsureLayer(int level)
    {
        if (!_layers.ContainsKey(level)) _layers[level] = new Dictionary<long, List<long>>();
    }

    #endregion
}

#endregion