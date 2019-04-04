using System;

namespace Core.Data.Index;

/// <summary>
///     标记属性为索引列，加速查询性能。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class IndexAttribute : Attribute
{
    /// <summary>
    ///     索引名称。
    /// </summary>
    public string? name { get; set; }
}