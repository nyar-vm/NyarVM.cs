using System;

namespace Core.Flow.Workflow;

/// <summary>
///     标记一个类为工作流定义
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WorkflowAttribute : Attribute
{
    /// <summary>
    ///     工作流名称
    /// </summary>
    public string? name { get; set; }
}