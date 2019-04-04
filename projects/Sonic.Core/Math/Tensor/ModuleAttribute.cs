using System;

namespace Core.Math.Tensor;

/// <summary>
///     标记类型为计算模块的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleAttribute : Attribute
{
}