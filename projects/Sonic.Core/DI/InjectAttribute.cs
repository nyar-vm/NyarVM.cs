using System;

namespace Core.DI;

/// <summary>
///     标记构造函数、属性或字段为依赖注入目标
/// </summary>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class InjectAttribute : Attribute
{
}