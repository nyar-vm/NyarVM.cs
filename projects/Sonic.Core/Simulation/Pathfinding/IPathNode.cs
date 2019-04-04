namespace Core.Simulation.Pathfinding;

/// <summary>
///     IPathNode 接口
/// </summary>
public interface IPathNode
{
    /// <summary>
    ///     X 坐标
    /// </summary>
    float x { get; }

    /// <summary>
    ///     Y 坐标
    /// </summary>
    float y { get; }

    /// <summary>
    ///     Z 坐标
    /// </summary>
    float z { get; }

    /// <summary>
    ///     是否可通行
    /// </summary>
    bool walkable { get; }
}