using Core.Simulation.Pathfinding;

namespace Std.Simulation.Pathfinding;

/// <summary>
///     路径节点，实现 IPathNode 接口，表示寻路网格中的一个节点
/// </summary>
public sealed class PathNode : IPathNode
{
    /// <summary>
    ///     初始化路径节点
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="walkable">是否可通行</param>
    public PathNode(float x, float y, float z, bool walkable = true)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.walkable = walkable;
    }

    /// <summary>
    ///     X 坐标
    /// </summary>
    public float x { get; }

    /// <summary>
    ///     Y 坐标
    /// </summary>
    public float y { get; }

    /// <summary>
    ///     Z 坐标
    /// </summary>
    public float z { get; }

    /// <summary>
    ///     是否可通行
    /// </summary>
    public bool walkable { get; set; }
}