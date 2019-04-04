namespace Core.Simulation.ECS;

/// <summary>
///     IEntity 接口
/// </summary>
public interface IEntity
{
    /// <summary>
    ///     实体唯一标识
    /// </summary>
    long id { get; }
}