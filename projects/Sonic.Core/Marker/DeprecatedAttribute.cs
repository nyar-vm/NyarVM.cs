using System;

namespace Core.Marker;

/// <summary>
///     标记属性已弃用，并附带弃用说明信息。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DeprecatedAttribute : Attribute
{
    /// <summary>
    ///     弃用说明信息。
    /// </summary>
    public string? message { get; set; }
}