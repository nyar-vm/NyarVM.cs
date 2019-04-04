using System;

namespace Core.Simulation.Physics;

/// <summary>
///     Collider 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ColliderAttribute : Attribute
{
    /// <summary>
    ///     碰撞体形状
    /// </summary>
    public ColliderShape shape { get; set; } = ColliderShape.sphere;
}