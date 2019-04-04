namespace Std.DL.Cluster;

/// <summary>节点分配与回收</summary>
public interface INodeProvider
{
    /// <summary>分配集群节点</summary>
    Task<ClusterNode> AllocateAsync(ResourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>释放集群节点</summary>
    Task ReleaseAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>列出所有节点</summary>
    IAsyncEnumerable<ClusterNode> ListAsync(CancellationToken cancellationToken = default);
}