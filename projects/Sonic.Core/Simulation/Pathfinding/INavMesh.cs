using System.Collections.Generic;

namespace Core.Simulation.Pathfinding;

/// <summary>
///     INavMesh 接口
/// </summary>
public interface INavMesh
{
    /// <summary>
    ///     在导航网格上寻路
    /// </summary>
    IReadOnlyList<IPathNode> find_path(IPathNode start, IPathNode end);
}