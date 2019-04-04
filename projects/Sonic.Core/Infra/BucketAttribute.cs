using System;

namespace Core.Infra;

/// <summary>
///     标记一个字段或属性为存储桶资源
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BucketAttribute : Attribute
{
    /// <summary>
    ///     存储桶名称
    /// </summary>
    public string? name { get; set; }
}