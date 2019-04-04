namespace Std.DL.Topos;

internal sealed class TopologyBuilder : ITopologyBuilder
{
    private readonly List<TopologyEdge> _edges = [];
    private readonly string _name;
    private readonly List<TopologyNode> _nodes = [];

    public TopologyBuilder(string name)
    {
        _name = name;
    }

    public ITopologyBuilder AddNode(string id, string type, params (string Name, string DataType, bool IsInput)[] ports)
    {
        _nodes.Add(new TopologyNode
        {
            Id = id,
            Type = type,
            Ports = [.. ports.Select(p => new Port { Name = p.Name, DataType = p.DataType, IsInput = p.IsInput })]
        });
        return this;
    }

    public ITopologyBuilder AddEdge(string sourceNodeId, string sourcePort, string targetNodeId, string targetPort)
    {
        _edges.Add(new TopologyEdge
        {
            SourceNodeId = sourceNodeId,
            SourcePortName = sourcePort,
            TargetNodeId = targetNodeId,
            TargetPortName = targetPort
        });
        return this;
    }

    public ITopologyBuilder WithProperty(string nodeId, string key, object value)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null) node.Properties[key] = value;
        return this;
    }

    public ITopology Build()
    {
        return new TopologyImpl(_name, _nodes.AsReadOnly(), _edges.AsReadOnly());
    }
}