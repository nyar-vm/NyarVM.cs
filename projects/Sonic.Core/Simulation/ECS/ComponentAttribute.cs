using System;

namespace Core.Simulation.ECS;

/// <summary>
///     Component 属性
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class ComponentAttribute : Attribute
{
}