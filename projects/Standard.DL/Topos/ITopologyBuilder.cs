namespace Std.DL.Topos;

/// <summary>声明式链式构建器</summary>
public interface ITopologyBuilder
{
    /// <summary>添加节点</summary>
    ITopologyBuilder AddNode(string id, string type, params (string Name, string DataType, bool IsInput)[] ports);

    /// <summary>添加边</summary>
    ITopologyBuilder AddEdge(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort);

    /// <summary>设置节点属性</summary>
    ITopologyBuilder WithProperty(string nodeId, string key, object value);

    /// <summary>构建拓扑</summary>
    ITopology Build();
}