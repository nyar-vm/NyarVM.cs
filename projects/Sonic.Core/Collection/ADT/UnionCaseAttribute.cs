using System;

namespace Core.Collection.ADT;

/// <summary>
///     标记判别联合的变体成员，指定该成员在模式匹配中的名称
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UnionCaseAttribute : Attribute
{
    /// <summary>
    ///     变体名称，为 null 时使用成员名称
    /// </summary>
    public string? name { get; set; }
}