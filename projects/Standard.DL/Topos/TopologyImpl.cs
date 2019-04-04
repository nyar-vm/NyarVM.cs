namespace Std.DL.Topos;

internal sealed class TopologyImpl : ITopology
{
    public TopologyImpl()
    {
        Name = "";
        Nodes = [];
        Edges = [];
    }

    public TopologyImpl(string name, IReadOnlyList<ITopologyNode> nodes, IReadOnlyList<ITopologyEdge> edges)
    {
        Name = name;
        Nodes = nodes;
        Edges = edges;
    }

    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; }
    public IReadOnlyList<ITopologyNode> Nodes { get; }
    public IReadOnlyList<ITopologyEdge> Edges { get; }

    public byte[] Serialize()
    {
        return [];
    }
}