using Std.DL.Flux;

namespace Std.DL.Cluster;

/// <summary>
///     进程内张量传输层 —— 用于本地测试和模拟多节点通信，
///     基于内存字典实现节点间张量传递
/// </summary>
public sealed class InProcessTransport : IFluxTransport
{
    private readonly Dictionary<string, ArrayND> _buffers = new();
    private readonly object _lock = new();

    /// <summary>
    ///     缓冲区中待接收的张量数量
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _buffers.Count;
            }
        }
    }

    /// <summary>
    ///     发送张量到目标节点
    /// </summary>
    /// <param name="sourceNodeId">源节点 ID</param>
    /// <param name="targetNodeId">目标节点 ID</param>
    /// <param name="arrayNd">待发送张量</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SendAsync(string sourceNodeId, string targetNodeId, ArrayND arrayNd,
        CancellationToken cancellationToken = default)
    {
        var key = $"{targetNodeId}:inbox";
        lock (_lock)
        {
            _buffers[key] = arrayNd.Clone();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     从节点接收张量
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收到的张量</returns>
    public Task<ArrayND> ReceiveAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        var key = $"{nodeId}:inbox";
        lock (_lock)
        {
            if (_buffers.Remove(key, out var tensor)) return Task.FromResult(tensor);
        }

        throw new InvalidOperationException($"节点 {nodeId} 没有待接收的张量");
    }

    /// <summary>
    ///     清空所有缓冲区
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _buffers.Clear();
        }
    }
}