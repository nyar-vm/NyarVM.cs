using System;

namespace Core.State.Observable;

/// <summary>
///     CombineLatest 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CombineLatestAttribute : Attribute
{
    /// <summary>
    ///     合并源属性名称
    /// </summary>
    public string? sources { get; set; }
}