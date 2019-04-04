using System;

namespace Core.Config;

/// <summary>
///     标记配置属性使用整体替换合并策略，新值完全覆盖旧值。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OverwriteAttribute : Attribute
{
}