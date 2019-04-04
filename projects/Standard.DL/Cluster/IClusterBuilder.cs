namespace Std.DL.Cluster;

/// <summary>集群配置构建器</summary>
public interface IClusterBuilder
{
    /// <summary>设置节点提供者</summary>
    IClusterBuilder WithNodeProvider(INodeProvider provider);

    /// <summary>设置传输层</summary>
    IClusterBuilder WithTransport(IFluxTransport transport);

    /// <summary>设置最大节点数</summary>
    IClusterBuilder WithMaxNodes(int count);

    /// <summary>构建集群</summary>
    ICluster Build();
}