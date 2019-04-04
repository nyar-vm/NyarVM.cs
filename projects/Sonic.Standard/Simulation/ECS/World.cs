using Core.Simulation.ECS;

namespace Std.Simulation.ECS;

/// <summary>
///     ECS 世界，实现 IWorld 接口，管理实体的创建、销毁和查询
/// </summary>
public sealed class World : IWorld
{
    /// <summary>
    ///     实体列表
    /// </summary>
    private readonly List<EcsEntity> _entities = [];

    /// <summary>
    ///     实体 ID 计数器
    /// </summary>
    private long _next_id = 1;

    /// <summary>
    ///     创建新实体
    /// </summary>
    /// <returns>创建的实体</returns>
    public IEntity create_entity()
    {
        var entity = new EcsEntity(Interlocked.Increment(ref _next_id));
        _entities.Add(entity);
        return entity;
    }

    /// <summary>
    ///     销毁实体
    /// </summary>
    /// <param name="entity">要销毁的实体</param>
    public void destroy_entity(IEntity entity)
    {
        if (entity is EcsEntity e) _entities.Remove(e);
    }

    /// <summary>
    ///     查询拥有指定组件的实体
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <returns>匹配的实体列表</returns>
    public IEnumerable<T> query<T>()
    {
        yield break;
    }
}