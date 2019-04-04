using Std.DL.Flux;

namespace Std.DL.Cluster;

/// <summary>
///     ZeRO 阶段枚举 —— 定义 ZeRO 分布式训练的分片级别
/// </summary>
public enum ZeROStage
{
    /// <summary>
    ///     ZeRO-1：优化器状态分片
    ///     每个 rank 仅维护本地分片的优化器状态（如动量、方差），
    ///     梯度通过 AllReduce 同步，内存节省与 WorldSize 成正比
    /// </summary>
    Stage1 = 1,

    /// <summary>
    ///     ZeRO-2：梯度分片
    ///     在 ZeRO-1 基础上进一步分片梯度，
    ///     通过 Reduce-Scatter 使每个 rank 仅保留本地分片的梯度，
    ///     内存节省为优化器状态 + 梯度两部分，与 WorldSize 成正比
    /// </summary>
    Stage2 = 2
}

/// <summary>
///     ZeRO 优化器 —— 包装现有优化器实现 ZeRO-1/2 分布式训练
///     ZeRO-1：优化器状态按 rank 分片，梯度通过 AllReduce 同步后仅更新本地分片
///     ZeRO-2：优化器状态和梯度均按 rank 分片，梯度通过 Reduce-Scatter 分片后仅更新本地分片
///     两种阶段在参数更新后均通过 All-Gather 同步参数
/// </summary>
public sealed class ZeROOptimizer : IOptimizer
{
    #region 构造函数

    /// <summary>
    ///     创建 ZeRO 优化器
    /// </summary>
    /// <param name="baseOptimizer">基础优化器（如 SGD、Adam）</param>
    /// <param name="communicator">集合通信器</param>
    /// <param name="stage">ZeRO 阶段</param>
    public ZeROOptimizer(IOptimizer baseOptimizer, ICollectiveCommunicator communicator, ZeROStage stage)
    {
        _baseOptimizer = baseOptimizer;
        _communicator = communicator;
        _stage = stage;
    }

    #endregion

    #region 分片计算

    /// <summary>
    ///     计算指定 rank 的分片范围
    ///     分片按连续块划分，每块大小为 ceil(totalParams / worldSize)，
    ///     最后一个 rank 的分片可能小于最大块大小
    /// </summary>
    /// <param name="totalParams">参数总数</param>
    /// <param name="rank">节点排名</param>
    /// <returns>分片的起止索引（左闭右开）</returns>
    public (int Start, int End) GetShardRange(int totalParams, int rank)
    {
        var worldSize = _communicator.WorldSize;
        var maxShardSize = (totalParams + worldSize - 1) / worldSize;
        var start = rank * maxShardSize;
        var end = System.Math.Min((rank + 1) * maxShardSize, totalParams);
        return (start, end);
    }

    #endregion

    #region 分片参数

    /// <summary>
    ///     分片参数 —— 包装本地分片的值和梯度，供基础优化器使用
    ///     独立维护 Grad 属性，不依赖 ArrayND 的自动微分梯度
    /// </summary>
    private sealed class ShardParameter : IParameter
    {
        /// <summary>
        ///     创建分片参数
        /// </summary>
        /// <param name="size">分片大小</param>
        public ShardParameter(int size)
        {
            Value = ArrayND.Zeros(size);
            WritableGrad = ArrayND.Zeros(size);
        }

        /// <summary>
        ///     可写梯度引用（用于外部写入梯度数据）
        /// </summary>
        public ArrayND WritableGrad { get; }

        /// <summary>
        ///     参数值
        /// </summary>
        public ArrayND Value { get; }

        /// <summary>
        ///     参数梯度（覆盖 IParameter 默认实现，使用独立梯度缓冲区）
        /// </summary>
        public ArrayND? Grad => WritableGrad;
    }

    #endregion

    #region 字段

    private readonly IOptimizer _baseOptimizer;
    private readonly ICollectiveCommunicator _communicator;
    private readonly ZeROStage _stage;

    private ShardParameter? _shardParam;
    private int _totalParamCount;
    private int _paddedParamCount;
    private int _maxShardSize;
    private bool _initialized;

    private float[]? _paramBuffer;
    private float[]? _gradBuffer;
    private float[]? _shardSendBuffer;
    private float[]? _allGatherRecvBuffer;

    #endregion

    #region IOptimizer 实现

    /// <summary>
    ///     执行一步 ZeRO 优化：
    ///     1. 展平参数值和梯度到连续缓冲区
    ///     2. 梯度通信（ZeRO-1: AllReduce 均值, ZeRO-2: Reduce-Scatter 均值）
    ///     3. 复制本地分片的值和梯度到分片参数
    ///     4. 调用基础优化器更新分片参数
    ///     5. All-Gather 同步更新后的参数
    ///     6. 写回参数值到原始参数
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void Step(IEnumerable<IParameter> parameters)
    {
        var paramList = parameters as IReadOnlyList<IParameter> ?? [.. parameters];

        if (paramList.Count == 0) return;

        EnsureInitialized(paramList);

        var paramBuf = _paramBuffer!;
        var gradBuf = _gradBuffer!;

        FlattenValues(paramList, paramBuf);
        FlattenGradients(paramList, gradBuf);

        if (_stage == ZeROStage.Stage1)
            _communicator.AllReduceAsync(gradBuf, ReduceOp.Avg).GetAwaiter().GetResult();
        else
            _communicator.ReduceScatterAsync(gradBuf, ReduceOp.Avg).GetAwaiter().GetResult();

        var (start, end) = GetShardRange(_totalParamCount, _communicator.Rank);
        var shardSize = end - start;

        var shardValueSpan = _shardParam!.Value.AsWriteSpan();
        var shardGradSpan = _shardParam!.WritableGrad.AsWriteSpan();

        for (var i = 0; i < shardSize; i++) shardValueSpan[i] = paramBuf[start + i];

        var gradOffset = _stage == ZeROStage.Stage1 ? start : 0;
        for (var i = 0; i < shardSize; i++) shardGradSpan[i] = gradBuf[gradOffset + i];

        _baseOptimizer.Step([_shardParam]);

        var sendBuf = _shardSendBuffer!;
        var recvBuf = _allGatherRecvBuffer!;

        for (var i = 0; i < shardSize; i++) paramBuf[start + i] = shardValueSpan[i];

        sendBuf.AsSpan().Clear();
        for (var i = 0; i < shardSize; i++) sendBuf[i] = paramBuf[start + i];

        _communicator.AllGatherAsync(sendBuf, recvBuf).GetAwaiter().GetResult();

        for (var r = 0; r < _communicator.WorldSize; r++)
        {
            var (rStart, rEnd) = GetShardRange(_totalParamCount, r);
            var rSize = rEnd - rStart;
            var recvOffset = r * _maxShardSize;

            for (var i = 0; i < rSize; i++) paramBuf[rStart + i] = recvBuf[recvOffset + i];
        }

        UnflattenValues(paramList, paramBuf);
    }

    /// <summary>
    ///     清零所有参数的梯度，同时清零分片参数的梯度
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void ZeroGrad(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters) param.Value.ZeroGrad();

        if (_shardParam is not null) _shardParam.WritableGrad.AsWriteSpan().Clear();
    }

    #endregion

    #region 内部方法

    /// <summary>
    ///     延迟初始化分片参数和通信缓冲区
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    private void EnsureInitialized(IReadOnlyList<IParameter> parameters)
    {
        if (_initialized) return;

        _totalParamCount = 0;
        foreach (var p in parameters) _totalParamCount += p.Value.AsSpan().Length;

        var worldSize = _communicator.WorldSize;
        _maxShardSize = (_totalParamCount + worldSize - 1) / worldSize;
        _paddedParamCount = _maxShardSize * worldSize;

        var (shardStart, shardEnd) = GetShardRange(_totalParamCount, _communicator.Rank);
        var shardSize = shardEnd - shardStart;

        _shardParam = new ShardParameter(shardSize);
        _paramBuffer = new float[_paddedParamCount];
        _gradBuffer = new float[_paddedParamCount];
        _shardSendBuffer = new float[_maxShardSize];
        _allGatherRecvBuffer = new float[_paddedParamCount];

        _initialized = true;
    }

    /// <summary>
    ///     将所有参数值展平到连续缓冲区，并清零尾部填充区域
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    /// <param name="buffer">目标缓冲区</param>
    private static void FlattenValues(IReadOnlyList<IParameter> parameters, float[] buffer)
    {
        var offset = 0;
        foreach (var p in parameters)
        {
            var span = p.Value.AsSpan();
            span.CopyTo(buffer.AsSpan(offset, span.Length));
            offset += span.Length;
        }

        buffer.AsSpan(offset).Clear();
    }

    /// <summary>
    ///     将所有参数梯度展平到连续缓冲区，无梯度的参数填充零，并清零尾部填充区域
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    /// <param name="buffer">目标缓冲区</param>
    private static void FlattenGradients(IReadOnlyList<IParameter> parameters, float[] buffer)
    {
        var offset = 0;
        foreach (var p in parameters)
        {
            var grad = p.Grad;
            if (grad is not null)
            {
                var span = grad.AsSpan();
                span.CopyTo(buffer.AsSpan(offset, span.Length));
            }
            else
            {
                buffer.AsSpan(offset, p.Value.AsSpan().Length).Clear();
            }

            offset += p.Value.AsSpan().Length;
        }

        buffer.AsSpan(offset).Clear();
    }

    /// <summary>
    ///     将连续缓冲区的值写回各个参数
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    /// <param name="buffer">源缓冲区</param>
    private static void UnflattenValues(IReadOnlyList<IParameter> parameters, float[] buffer)
    {
        var offset = 0;
        foreach (var p in parameters)
        {
            var span = p.Value.AsWriteSpan();
            buffer.AsSpan(offset, span.Length).CopyTo(span);
            offset += span.Length;
        }
    }

    #endregion
}