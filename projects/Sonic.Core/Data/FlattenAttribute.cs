using System;

namespace Core.Data;

/// <summary>
///     将嵌套对象展平到当前对象层级。
///     `config` 和 `serde` 可以共享这份结构元信息。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class FlattenAttribute : Attribute
{
}
