namespace Std.DL.Cluster;

/// <summary>集群构建器实现</summary>
public sealed class ClusterBuilder : IClusterBuilder
{
    private int _maxNodes = 10;
    private INodeProvider? _nodeProvider;
    private IFluxTransport? _transport;

    /// <summary>设置节点提供者</summary>
    public IClusterBuilder WithNodeProvider(INodeProvider provider)
    {
        _nodeProvider = provider;
        return this;
    }

    /// <summary>设置传输层</summary>
    public IClusterBuilder WithTransport(IFluxTransport transport)
    {
        _transport = transport;
        return this;
    }

    /// <summary>设置最大节点数</summary>
    public IClusterBuilder WithMaxNodes(int count)
    {
        _maxNodes = count;
        return this;
    }

    /// <summary>构建集群</summary>
    public ICluster Build()
    {
        return new Cluster();
    }
}