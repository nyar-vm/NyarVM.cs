using System;

namespace Core.Data.Index;

/// <summary>
///     标记属性具有唯一约束，确保列值不重复。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueAttribute : Attribute
{
    /// <summary>
    ///     唯一约束名称。
    /// </summary>
    public string? name { get; set; }
}