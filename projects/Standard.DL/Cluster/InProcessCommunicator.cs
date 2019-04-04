using System.Collections.Concurrent;

namespace Std.DL.Cluster;

/// <summary>
///     进程内通信器 —— 在单进程中模拟多节点集合通信
///     使用共享内存（ConcurrentDictionary + BlockingCollection）实现
///     适用于单机多 GPU 或单元测试场景
/// </summary>
public sealed class InProcessCommunicator : ICollectiveCommunicator
{
    private readonly InProcessCommunicatorGroup _group;

    /// <summary>
    ///     创建进程内通信器
    /// </summary>
    /// <param name="rank">节点排名</param>
    /// <param name="worldSize">总节点数</param>
    /// <param name="group">通信器组（同一组的通信器共享内存）</param>
    internal InProcessCommunicator(int rank, int worldSize, InProcessCommunicatorGroup group)
    {
        Rank = rank;
        WorldSize = worldSize;
        _group = group;
    }

    /// <summary>
    ///     当前节点的排名
    /// </summary>
    public int Rank { get; }

    /// <summary>
    ///     参与通信的总节点数
    /// </summary>
    public int WorldSize { get; }

    /// <summary>
    ///     全局规约 —— 在进程内直接对共享缓冲区执行规约操作
    /// </summary>
    /// <param name="data">本节点参与规约的数据</param>
    /// <param name="op">规约操作类型</param>
    public async Task AllReduceAsync(float[] data, ReduceOp op)
    {
        await _group.BarrierAsync(Rank).ConfigureAwait(false);

        _group.SubmitData(Rank, data);

        await _group.WaitForAllAsync().ConfigureAwait(false);

        var allData = _group.GetAllData();
        var result = ApplyReduceOp(allData, op);

        Array.Copy(result, data, data.Length);

        _group.Reset();
        await _group.BarrierAsync(Rank).ConfigureAwait(false);
    }

    /// <summary>
    ///     规约散射 —— 先规约再按 rank 分片
    /// </summary>
    /// <param name="data">本节点的完整数据</param>
    /// <param name="op">规约操作类型</param>
    public async Task ReduceScatterAsync(float[] data, ReduceOp op)
    {
        await _group.BarrierAsync(Rank).ConfigureAwait(false);

        _group.SubmitData(Rank, data);

        await _group.WaitForAllAsync().ConfigureAwait(false);

        var allData = _group.GetAllData();
        var reduced = ApplyReduceOp(allData, op);

        var chunkSize = data.Length / WorldSize;
        var srcOffset = Rank * chunkSize;
        Array.Copy(reduced, srcOffset, data, 0, chunkSize);

        _group.Reset();
        await _group.BarrierAsync(Rank).ConfigureAwait(false);
    }

    /// <summary>
    ///     全局收集 —— 收集所有节点数据并拼接
    /// </summary>
    /// <param name="sendData">本节点发送的数据</param>
    /// <param name="recvBuffer">接收缓冲区</param>
    public async Task AllGatherAsync(float[] sendData, float[] recvBuffer)
    {
        await _group.BarrierAsync(Rank).ConfigureAwait(false);

        _group.SubmitData(Rank, sendData);

        await _group.WaitForAllAsync().ConfigureAwait(false);

        var allData = _group.GetAllData();
        var offset = 0;
        for (var i = 0; i < WorldSize; i++)
        {
            Array.Copy(allData[i], 0, recvBuffer, offset, allData[i].Length);
            offset += allData[i].Length;
        }

        _group.Reset();
        await _group.BarrierAsync(Rank).ConfigureAwait(false);
    }

    /// <summary>
    ///     广播 —— 从根节点复制数据到所有节点
    /// </summary>
    /// <param name="data">广播数据</param>
    /// <param name="rootRank">广播源节点</param>
    public async Task BroadcastAsync(float[] data, int rootRank)
    {
        await _group.BarrierAsync(Rank).ConfigureAwait(false);

        if (Rank == rootRank) _group.SubmitData(Rank, data);

        await _group.WaitForAllAsync().ConfigureAwait(false);

        var rootData = _group.GetData(rootRank);
        if (Rank != rootRank) Array.Copy(rootData, data, data.Length);

        _group.Reset();
        await _group.BarrierAsync(Rank).ConfigureAwait(false);
    }

    /// <summary>
    ///     点对点发送 —— 通过共享通道发送数据
    /// </summary>
    /// <param name="data">待发送的数据</param>
    /// <param name="destRank">目标节点</param>
    public async Task SendAsync(float[] data, int destRank)
    {
        var channel = _group.GetChannel(Rank, destRank);
        channel.Add(data);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    ///     点对点接收 —— 从共享通道接收数据
    /// </summary>
    /// <param name="buffer">接收缓冲区</param>
    /// <param name="srcRank">源节点</param>
    public async Task RecvAsync(float[] buffer, int srcRank)
    {
        var channel = _group.GetChannel(srcRank, Rank);
        var received = channel.Take();
        Array.Copy(received, buffer, buffer.Length);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
    }

    private static float[] ApplyReduceOp(List<float[]> allData, ReduceOp op)
    {
        var length = allData[0].Length;
        var result = new float[length];

        switch (op)
        {
            case ReduceOp.Sum:
            {
                for (var i = 0; i < allData.Count; i++)
                for (var j = 0; j < length; j++)
                    result[j] += allData[i][j];

                break;
            }
            case ReduceOp.Avg:
            {
                for (var i = 0; i < allData.Count; i++)
                for (var j = 0; j < length; j++)
                    result[j] += allData[i][j];

                var invN = 1.0f / allData.Count;
                for (var j = 0; j < length; j++) result[j] *= invN;
                break;
            }
            case ReduceOp.Max:
            {
                Array.Copy(allData[0], result, length);
                for (var i = 1; i < allData.Count; i++)
                for (var j = 0; j < length; j++)
                    if (allData[i][j] > result[j])
                        result[j] = allData[i][j];

                break;
            }
            case ReduceOp.Min:
            {
                Array.Copy(allData[0], result, length);
                for (var i = 1; i < allData.Count; i++)
                for (var j = 0; j < length; j++)
                    if (allData[i][j] < result[j])
                        result[j] = allData[i][j];

                break;
            }
        }

        return result;
    }
}

/// <summary>
///     进程内通信器组 —— 管理一组共享内存的通信器
///     同一组的通信器可以相互通信
/// </summary>
public sealed class InProcessCommunicatorGroup
{
    private readonly SemaphoreSlim _barrier;
    private readonly ConcurrentDictionary<(int src, int dst), BlockingCollection<float[]>> _channels;
    private readonly float[][] _nodeData;
    private readonly int _worldSize;
    private int _arrivedCount;
    private int _submittedCount;

    /// <summary>
    ///     创建进程内通信器组
    /// </summary>
    /// <param name="worldSize">总节点数</param>
    public InProcessCommunicatorGroup(int worldSize)
    {
        _worldSize = worldSize;
        _nodeData = new float[worldSize][];
        _channels = new ConcurrentDictionary<(int, int), BlockingCollection<float[]>>();
        _barrier = new SemaphoreSlim(0);
        _arrivedCount = 0;
        _submittedCount = 0;
    }

    /// <summary>
    ///     创建指定 rank 的通信器
    /// </summary>
    /// <param name="rank">节点排名</param>
    /// <returns>进程内通信器实例</returns>
    public InProcessCommunicator CreateCommunicator(int rank)
    {
        return new InProcessCommunicator(rank, _worldSize, this);
    }

    /// <summary>
    ///     提交节点数据
    /// </summary>
    /// <param name="rank">节点排名</param>
    /// <param name="data">节点数据</param>
    internal void SubmitData(int rank, float[] data)
    {
        _nodeData[rank] = data;
        if (Interlocked.Increment(ref _submittedCount) == _worldSize) _barrier.Release(_worldSize);
    }

    /// <summary>
    ///     获取指定节点的数据
    /// </summary>
    /// <param name="rank">节点排名</param>
    /// <returns>节点数据</returns>
    internal float[] GetData(int rank)
    {
        return _nodeData[rank];
    }

    /// <summary>
    ///     获取所有节点的数据
    /// </summary>
    /// <returns>所有节点数据列表</returns>
    internal List<float[]> GetAllData()
    {
        var result = new List<float[]>();
        for (var i = 0; i < _worldSize; i++) result.Add(_nodeData[i]);
        return result;
    }

    /// <summary>
    ///     获取或创建点对点通信通道
    /// </summary>
    /// <param name="src">源节点</param>
    /// <param name="dst">目标节点</param>
    /// <returns>通信通道</returns>
    internal BlockingCollection<float[]> GetChannel(int src, int dst)
    {
        return _channels.GetOrAdd((src, dst), _ => new BlockingCollection<float[]>(10));
    }

    /// <summary>
    ///     屏障同步 —— 等待所有节点到达
    /// </summary>
    /// <param name="rank">调用方节点排名</param>
    internal async Task BarrierAsync(int rank)
    {
        if (Interlocked.Increment(ref _arrivedCount) == _worldSize) _barrier.Release(_worldSize);

        await _barrier.WaitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     等待所有节点提交数据
    /// </summary>
    internal async Task WaitForAllAsync()
    {
        if (_submittedCount >= _worldSize) return;

        await _barrier.WaitAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     重置内部状态，为下一轮通信做准备
    /// </summary>
    internal void Reset()
    {
        Interlocked.Exchange(ref _arrivedCount, 0);
        Interlocked.Exchange(ref _submittedCount, 0);
    }
}