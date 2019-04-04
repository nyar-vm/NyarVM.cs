namespace Core.Simulation.Physics;

/// <summary>
///     IPhysicsWorld 接口
/// </summary>
public interface IPhysicsWorld
{
    /// <summary>
    ///     执行物理模拟步进
    /// </summary>
    void step(float delta_time);
}