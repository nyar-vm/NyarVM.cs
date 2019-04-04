using System;

namespace Core.Simulation.Physics;

/// <summary>
///     RigidBody 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RigidBodyAttribute : Attribute
{
    /// <summary>
    ///     质量
    /// </summary>
    public float mass { get; set; } = 1.0f;
}