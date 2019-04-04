namespace Std.DL.Data;

/// <summary>
///     DataLoader 配置选项
/// </summary>
public sealed record DataLoaderOptions
{
    /// <summary>批次大小</summary>
    public int BatchSize { get; init; } = 32;

    /// <summary>是否在每个 epoch 开始时随机打乱顺序</summary>
    public bool Shuffle { get; init; } = true;

    /// <summary>预取批次数量（异步管线用）</summary>
    public int PrefetchCount { get; init; } = 1;

    /// <summary>允许丢弃最后不完整的批次</summary>
    public bool DropLast { get; init; } = false;

    /// <summary>
    ///     创建默认配置
    /// </summary>
    public static DataLoaderOptions Default => new();
}

/// <summary>
///     数据加载器 —— 负责将数据集分批次、乱序、迭代输出
///     支持同步迭代和 epoch 重置
/// </summary>
public sealed class DataLoader
{
    private readonly List<DataBatch> _currentBatches;
    private readonly IDataset _dataset;
    private readonly int[] _indices;
    private readonly DataLoaderOptions _options;
    private int _epoch;

    /// <summary>
    ///     创建数据加载器
    /// </summary>
    /// <param name="dataset">数据集</param>
    /// <param name="options">加载选项</param>
    public DataLoader(IDataset dataset, DataLoaderOptions? options = null)
    {
        _dataset = dataset;
        _options = options ?? DataLoaderOptions.Default;
        _indices = new int[dataset.Count];
        _currentBatches = [];

        for (var i = 0; i < _indices.Length; i++) _indices[i] = i;

        if (_options.Shuffle) ShuffleIndices();

        BuildBatches();
    }

    /// <summary>当前 epoch（从 1 开始）</summary>
    public int Epoch => _epoch + 1;

    /// <summary>总批次数量</summary>
    public int BatchCount => _currentBatches.Count;

    /// <summary>批次大小</summary>
    public int BatchSize => _options.BatchSize;

    /// <summary>样本总数</summary>
    public int SampleCount => _dataset.Count;

    /// <summary>
    ///     获取所有批次
    /// </summary>
    public IReadOnlyList<DataBatch> Batches => _currentBatches;

    /// <summary>
    ///     迭代进入下一个 epoch（重新乱序并重建批次）
    /// </summary>
    /// <returns>新 epoch 的批次列表</returns>
    public IReadOnlyList<DataBatch> NextEpoch()
    {
        _epoch++;

        if (_options.Shuffle) ShuffleIndices();

        BuildBatches();
        return _currentBatches;
    }

    /// <summary>
    ///     重置到初始状态（epoch=0）
    /// </summary>
    public void Reset()
    {
        _epoch = 0;

        for (var i = 0; i < _indices.Length; i++) _indices[i] = i;

        if (_options.Shuffle) ShuffleIndices();

        BuildBatches();
    }

    /// <summary>
    ///     随机打乱索引顺序
    /// </summary>
    private void ShuffleIndices()
    {
        var rng = new Random(42 + _epoch);
        for (var i = _indices.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (_indices[i], _indices[j]) = (_indices[j], _indices[i]);
        }
    }

    /// <summary>
    ///     重构批次数据
    /// </summary>
    private void BuildBatches()
    {
        _currentBatches.Clear();

        var totalSamples = _dataset.Count;
        var batchSize = _options.BatchSize;
        var numBatches = totalSamples / batchSize;

        if (!_options.DropLast && totalSamples % batchSize != 0) numBatches++;

        for (var b = 0; b < numBatches; b++)
        {
            var start = b * batchSize;
            var end = System.Math.Min(start + batchSize, totalSamples);
            var size = end - start;

            if (_options.DropLast && size < batchSize) break;

            var batchIndices = new int[size];
            Array.Copy(_indices, start, batchIndices, 0, size);

            var (inputs, labels) = _dataset.GetBatch(batchIndices);

            _currentBatches.Add(new DataBatch
            {
                Inputs = inputs,
                Labels = labels,
                BatchIndex = b,
                TotalSamples = totalSamples
            });
        }
    }
}