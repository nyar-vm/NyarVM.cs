using System;

namespace Core.AI;

/// <summary>
///     标记向量类型的属性或字段，指定维度数
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class VectorAttribute : Attribute
{
    /// <summary>
    ///     向量维度数
    /// </summary>
    public int dimensions { get; set; } = 128;
}