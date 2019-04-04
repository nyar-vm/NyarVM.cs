using Std.DL.Flux;

namespace Std.DL.Cluster;

/// <summary>跨节点张量传输</summary>
public interface IFluxTransport
{
    /// <summary>发送张量到目标节点</summary>
    Task SendAsync(string sourceNodeId, string targetNodeId, ArrayND arrayNd,
        CancellationToken cancellationToken = default);

    /// <summary>从指定节点接收张量</summary>
    Task<ArrayND> ReceiveAsync(string nodeId, CancellationToken cancellationToken = default);
}