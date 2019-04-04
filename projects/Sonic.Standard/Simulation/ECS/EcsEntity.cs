using Core.Simulation.ECS;

namespace Std.Simulation.ECS;

/// <summary>
///     ECS 实体，实现 IEntity 接口，表示世界中的一个实体
/// </summary>
public sealed class EcsEntity : IEntity
{
    /// <summary>
    ///     初始化实体
    /// </summary>
    /// <param name="id">实体唯一标识</param>
    public EcsEntity(long id)
    {
        this.id = id;
    }

    /// <summary>
    ///     实体唯一标识
    /// </summary>
    public long id { get; }
}