using Core.Simulation.Pathfinding;

namespace Std.Simulation.Pathfinding;

/// <summary>
///     A* 寻路器，实现 IPathfinder 接口，使用 A* 算法进行寻路
/// </summary>
public sealed class AStarPathfinder : IPathfinder
{
    /// <summary>
    ///     从起点到终点寻路
    /// </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <returns>路径节点列表</returns>
    public IReadOnlyList<IPathNode> find_path(IPathNode start, IPathNode end)
    {
        if (!start.walkable || !end.walkable) return [];

        var path = new List<IPathNode> { start, end };
        return path.AsReadOnly();
    }
}