using System;

namespace Core.Simulation.ECS;

/// <summary>
///     System 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SystemAttribute : Attribute
{
}