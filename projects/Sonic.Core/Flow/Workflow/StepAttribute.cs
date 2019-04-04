using System;

namespace Core.Flow.Workflow;

/// <summary>
///     标记一个方法为工作流步骤
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StepAttribute : Attribute
{
    /// <summary>
    ///     当前步骤依赖的步骤名称
    /// </summary>
    public string? depends_on { get; set; }
}