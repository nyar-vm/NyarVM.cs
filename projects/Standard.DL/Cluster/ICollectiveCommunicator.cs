namespace Std.DL.Cluster;

/// <summary>
///     规约操作类型
/// </summary>
public enum ReduceOp
{
    /// <summary>
    ///     求和
    /// </summary>
    Sum = 0,

    /// <summary>
    ///     求均值
    /// </summary>
    Avg = 1,

    /// <summary>
    ///     取最大值
    /// </summary>
    Max = 2,

    /// <summary>
    ///     取最小值
    /// </summary>
    Min = 3
}

/// <summary>
///     集合通信器接口 —— 分布式训练的通信原语抽象
///     提供 AllReduce、ReduceScatter、AllGather、Broadcast 等集合通信操作
///     以及 Send/Recv 点对点通信操作
/// </summary>
public interface ICollectiveCommunicator : IDisposable
{
    /// <summary>
    ///     当前节点的排名（0 ~ WorldSize-1）
    /// </summary>
    int Rank { get; }

    /// <summary>
    ///     参与通信的总节点数
    /// </summary>
    int WorldSize { get; }

    /// <summary>
    ///     全局规约 —— 对所有节点的数据执行规约操作，结果广播到所有节点
    ///     操作完成后，所有节点的 data 数组内容相同
    /// </summary>
    /// <param name="data">本节点参与规约的数据（操作完成后存储结果）</param>
    /// <param name="op">规约操作类型</param>
    Task AllReduceAsync(float[] data, ReduceOp op);

    /// <summary>
    ///     规约散射 —— 先对所有节点数据执行规约，再将结果按块散射到各节点
    ///     每个节点仅保留结果中对应自己 rank 的分片
    ///     data 数组长度必须能被 WorldSize 整除
    /// </summary>
    /// <param name="data">本节点的完整数据（操作完成后仅保留本地分片有效）</param>
    /// <param name="op">规约操作类型</param>
    Task ReduceScatterAsync(float[] data, ReduceOp op);

    /// <summary>
    ///     全局收集 —— 收集所有节点的数据，拼接后广播到所有节点
    ///     sendData 长度必须相同，recvBuffer 长度 = sendData.Length × WorldSize
    /// </summary>
    /// <param name="sendData">本节点发送的数据</param>
    /// <param name="recvBuffer">接收缓冲区（按 rank 顺序排列所有节点数据）</param>
    Task AllGatherAsync(float[] sendData, float[] recvBuffer);

    /// <summary>
    ///     广播 —— 从根节点向所有节点广播数据
    ///     非根节点的 data 数组将被覆盖为根节点的数据
    /// </summary>
    /// <param name="data">广播数据（根节点为源，其他节点为目标）</param>
    /// <param name="rootRank">广播源节点的 rank</param>
    Task BroadcastAsync(float[] data, int rootRank);

    /// <summary>
    ///     点对点发送 —— 向指定节点发送数据
    /// </summary>
    /// <param name="data">待发送的数据</param>
    /// <param name="destRank">目标节点的 rank</param>
    Task SendAsync(float[] data, int destRank);

    /// <summary>
    ///     点对点接收 —— 从指定节点接收数据
    /// </summary>
    /// <param name="buffer">接收缓冲区</param>
    /// <param name="srcRank">源节点的 rank</param>
    Task RecvAsync(float[] buffer, int srcRank);
}