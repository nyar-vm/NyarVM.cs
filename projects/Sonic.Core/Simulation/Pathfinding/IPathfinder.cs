using System.Collections.Generic;

namespace Core.Simulation.Pathfinding;

/// <summary>
///     IPathfinder 接口
/// </summary>
public interface IPathfinder
{
    /// <summary>
    ///     从起点到终点寻路
    /// </summary>
    IReadOnlyList<IPathNode> find_path(IPathNode start, IPathNode end);
}