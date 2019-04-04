using System;

namespace Core.Math.Tensor;

/// <summary>
///     标记方法支持自动微分求导的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AutogradAttribute : Attribute
{
}