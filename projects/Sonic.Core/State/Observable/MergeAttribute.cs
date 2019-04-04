using System;

namespace Core.State.Observable;

/// <summary>
///     Merge 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MergeAttribute : Attribute
{
    /// <summary>
    ///     合并源属性名称
    /// </summary>
    public string? sources { get; set; }
}