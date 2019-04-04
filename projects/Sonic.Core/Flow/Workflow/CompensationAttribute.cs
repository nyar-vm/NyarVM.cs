using System;

namespace Core.Flow.Workflow;

/// <summary>
///     标记一个方法为补偿操作，用于回滚指定步骤
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CompensationAttribute : Attribute
{
    /// <summary>
    ///     此补偿操作对应的步骤名称
    /// </summary>
    public string? for_step { get; set; }
}