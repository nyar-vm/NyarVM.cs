namespace Std.DL.Topos;

/// <summary>静态门面（Define / Load）</summary>
public static class Topology
{
    /// <summary>创建拓扑构建器</summary>
    public static ITopologyBuilder Define(string name)
    {
        return new TopologyBuilder(name);
    }

    /// <summary>从字节加载拓扑</summary>
    public static ITopology Load(byte[] data)
    {
        return new TopologyImpl();
    }
}