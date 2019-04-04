using System;

namespace Core.Data;

/// <summary>
///     标记属性在数据序列化和反序列化时忽略。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class IgnoreDataAttribute : Attribute
{
}
