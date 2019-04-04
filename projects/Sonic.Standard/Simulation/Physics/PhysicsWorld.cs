using Core.Simulation.Physics;

namespace Std.Simulation.Physics;

/// <summary>
///     物理世界，实现 IPhysicsWorld 接口，执行物理模拟步进
/// </summary>
public sealed class PhysicsWorld : IPhysicsWorld
{
    /// <summary>
    ///     物理体列表
    /// </summary>
    private readonly List<IPhysicsBody> _bodies = [];

    /// <summary>
    ///     执行物理模拟步进
    /// </summary>
    /// <param name="delta_time">时间步长</param>
    public void step(float deltaTime)
    {
        foreach (var body in _bodies)
        {
        }
    }

    /// <summary>
    ///     添加物理体到世界
    /// </summary>
    /// <param name="body">物理体</param>
    public void add_body(IPhysicsBody body)
    {
        _bodies.Add(body);
    }
}