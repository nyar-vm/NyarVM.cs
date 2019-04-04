using System;

namespace Core.Data;

/// <summary>
///     标记属性自指定版本起引入。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SinceAttribute : Attribute
{
    /// <summary>
    ///     引入该属性的版本号。
    /// </summary>
    public int version { get; set; }
}