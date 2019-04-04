using System;

namespace Core.State.Observable;

/// <summary>
///     Switch 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SwitchAttribute : Attribute
{
    /// <summary>
    ///     切换源属性名称
    /// </summary>
    public string? source { get; set; }
}