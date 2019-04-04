namespace Core.Simulation.Physics;

/// <summary>
///     ICollisionCallback 接口
/// </summary>
public interface ICollisionCallback
{
    /// <summary>
    ///     碰撞回调
    /// </summary>
    void on_collision(IPhysicsBody other);
}