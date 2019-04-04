using System;

namespace Core.Config;

/// <summary>
///     验证集合属性不能为空。验证时检查集合是否包含至少一个元素�?///
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CollectionNotEmptyAttribute : Attribute
{
}