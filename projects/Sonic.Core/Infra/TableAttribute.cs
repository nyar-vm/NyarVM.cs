using System;

namespace Core.Infra;

/// <summary>
///     标记一个字段或属性为数据表资源
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class TableAttribute : Attribute
{
    /// <summary>
    ///     表名称
    /// </summary>
    public string? name { get; set; }
}