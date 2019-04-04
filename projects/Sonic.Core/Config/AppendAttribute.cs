using System;

namespace Core.Config;

/// <summary>
///     标记列表属性使用追加合并策略，新源中的元素将追加到现有列表末尾�?///
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AppendAttribute : Attribute
{
}