using System.Collections.Generic;

namespace Core.Simulation.ECS;

/// <summary>
///     IWorld 接口
/// </summary>
public interface IWorld
{
    /// <summary>
    ///     创建新实体
    /// </summary>
    IEntity create_entity();

    /// <summary>
    ///     销毁实体
    /// </summary>
    void destroy_entity(IEntity entity);

    /// <summary>
    ///     查询拥有指定组件的实体
    /// </summary>
    IEnumerable<T> query<T>();
}