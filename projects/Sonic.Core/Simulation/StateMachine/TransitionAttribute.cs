using System;

namespace Core.Simulation.StateMachine;

/// <summary>
///     Transition 属性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TransitionAttribute : Attribute
{
    /// <summary>
    ///     源状态名称
    /// </summary>
    public string? from { get; set; }


    /// <summary>
    ///     目标状态名称
    /// </summary>
    public string? to { get; set; }


    /// <summary>
    ///     转换条件方法名
    /// </summary>
    public string? condition { get; set; }
}