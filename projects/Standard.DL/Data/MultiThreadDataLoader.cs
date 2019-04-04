namespace Std.DL.Data;

/// <summary>
///     多线程数据加载器 —— 使用后台线程池预准备批次数据
///     支持数据变换管线和多线程预取，减少训练时的数据等待时间
/// </summary>
public sealed class MultiThreadDataLoader : IDisposable
{
    /// <summary>
    ///     创建多线程数据加载器
    /// </summary>
    /// <param name="dataset">数据集</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="numWorkers">工作线程数</param>
    /// <param name="transform">可选的数据变换</param>
    public MultiThreadDataLoader(IDataset dataset, int batchSize, int numWorkers, ITransform? transform = null)
    {
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize), "批次大小必须大于零");

        if (numWorkers < 1) throw new ArgumentOutOfRangeException(nameof(numWorkers), "工作线程数必须大于零");

        _dataset = dataset;
        BatchSize = batchSize;
        NumWorkers = numWorkers;
        _transform = transform;
        _indices = new int[dataset.Count];
        _workers = new Thread[numWorkers];
        _prefetchQueue = new BlockingQueue<DataBatch>(numWorkers * 2);
        _indexLock = new object();
        _cancellation = new CancellationTokenSource();
        _nextSampleIndex = 0;
        _epoch = 0;

        for (var i = 0; i < _indices.Length; i++) _indices[i] = i;

        ShuffleIndices();
        StartWorkers();
    }

    /// <summary>
    ///     当前 epoch（从 1 开始）
    /// </summary>
    public int Epoch => _epoch + 1;

    /// <summary>
    ///     样本总数
    /// </summary>
    public int SampleCount => _dataset.Count;

    /// <summary>
    ///     批次大小
    /// </summary>
    public int BatchSize { get; }

    /// <summary>
    ///     工作线程数
    /// </summary>
    public int NumWorkers { get; }

    /// <summary>
    ///     释放资源，停止所有工作线程
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        StopWorkers();
        _prefetchQueue.Clear();
        _cancellation.Dispose();
    }

    /// <summary>
    ///     获取下一个预取好的批次（阻塞等待直到批次就绪）
    /// </summary>
    /// <returns>数据批次，若当前 epoch 已结束则返回 null</returns>
    public DataBatch? GetNextBatch()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _prefetchQueue.Dequeue();
    }

    /// <summary>
    ///     重置到起始位置，重新打乱数据顺序
    /// </summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        StopWorkers();
        _prefetchQueue.Clear();

        _epoch = 0;
        _nextSampleIndex = 0;

        for (var i = 0; i < _indices.Length; i++) _indices[i] = i;

        ShuffleIndices();
        StartWorkers();
    }

    #region 内部阻塞队列

    /// <summary>
    ///     线程安全的阻塞队列 —— 用于生产者-消费者模式
    ///     当队列为空时消费者阻塞等待，当队列已满时生产者阻塞等待
    /// </summary>
    private sealed class BlockingQueue<T> where T : class
    {
        private readonly int _capacity;
        private readonly object _lock;
        private readonly Queue<T> _queue;
        private bool _ended;

        /// <summary>
        ///     创建阻塞队列
        /// </summary>
        /// <param name="capacity">队列容量</param>
        public BlockingQueue(int capacity)
        {
            _capacity = capacity;
            _queue = new Queue<T>(capacity);
            _lock = new object();
            _ended = false;
        }

        /// <summary>
        ///     入队：如果队列已满则阻塞等待
        /// </summary>
        /// <param name="item">入队元素</param>
        /// <returns>是否成功入队</returns>
        public bool Enqueue(T item)
        {
            lock (_lock)
            {
                while (_queue.Count >= _capacity && !_ended) Monitor.Wait(_lock);

                if (_ended) return false;

                _queue.Enqueue(item);
                Monitor.PulseAll(_lock);
                return true;
            }
        }

        /// <summary>
        ///     出队：如果队列为空则阻塞等待
        /// </summary>
        /// <returns>出队元素，若队列已结束则返回 null</returns>
        public T? Dequeue()
        {
            lock (_lock)
            {
                while (_queue.Count == 0 && !_ended) Monitor.Wait(_lock);

                if (_queue.Count > 0)
                {
                    var item = _queue.Dequeue();
                    Monitor.PulseAll(_lock);
                    return item;
                }

                return null;
            }
        }

        /// <summary>
        ///     通知消费者队列已结束
        /// </summary>
        public void SignalEnd()
        {
            lock (_lock)
            {
                _ended = true;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        ///     清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
                _ended = false;
                Monitor.PulseAll(_lock);
            }
        }
    }

    #endregion

    #region 字段

    private readonly IDataset _dataset;
    private readonly ITransform? _transform;
    private readonly int[] _indices;
    private readonly Thread[] _workers;
    private readonly BlockingQueue<DataBatch> _prefetchQueue;
    private readonly object _indexLock;
    private readonly CancellationTokenSource _cancellation;

    private int _nextSampleIndex;
    private bool _disposed;
    private int _epoch;

    #endregion

    #region 私有方法

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
    ///     启动所有工作线程
    /// </summary>
    private void StartWorkers()
    {
        _nextSampleIndex = 0;

        for (var i = 0; i < NumWorkers; i++)
        {
            _workers[i] = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"DataLoader-Worker-{i}"
            };
            _workers[i].Start();
        }
    }

    /// <summary>
    ///     停止所有工作线程
    /// </summary>
    private void StopWorkers()
    {
        _cancellation.Cancel();
        _prefetchQueue.SignalEnd();

        foreach (var worker in _workers)
            if (worker is not null && worker.IsAlive)
                worker.Join(1000);
    }

    /// <summary>
    ///     工作线程主循环：从数据集中读取样本并组装批次
    /// </summary>
    private void WorkerLoop()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            int[] batchIndices;

            lock (_indexLock)
            {
                if (_nextSampleIndex >= _dataset.Count)
                {
                    _prefetchQueue.SignalEnd();
                    break;
                }

                var remaining = _dataset.Count - _nextSampleIndex;
                var currentBatchSize = System.Math.Min(BatchSize, remaining);
                batchIndices = new int[currentBatchSize];

                for (var i = 0; i < currentBatchSize; i++) batchIndices[i] = _indices[_nextSampleIndex + i];

                _nextSampleIndex += currentBatchSize;
            }

            var (inputs, labels) = _dataset.GetBatch(batchIndices);

            if (_transform is not null) inputs = _transform.Apply(inputs);

            var batch = new DataBatch
            {
                Inputs = inputs,
                Labels = labels,
                BatchIndex = -1,
                TotalSamples = _dataset.Count
            };

            if (!_prefetchQueue.Enqueue(batch)) break;
        }
    }

    #endregion
}