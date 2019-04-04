using System;

namespace Core.Simulation.StateMachine;

/// <summary>
///     State 属性
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class StateAttribute : Attribute
{
}