using System;

namespace Core.Data;

/// <summary>
///     标记数据库迁移类，指定迁移步长。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MigrationAttribute : Attribute
{
    /// <summary>
    ///     迁移步长，默认为 1。
    /// </summary>
    public int step { get; set; } = 1;
}