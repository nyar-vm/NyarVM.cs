namespace Core.Simulation.Physics;

/// <summary>
///     IPhysicsBody 接口
/// </summary>
public interface IPhysicsBody
{
    /// <summary>
    ///     物体质量
    /// </summary>
    float mass { get; }

    /// <summary>
    ///     碰撞体形状
    /// </summary>
    ColliderShape shape { get; }
}