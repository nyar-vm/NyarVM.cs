using System;

namespace Core.Math.Tensor;

/// <summary>
///     标记类型具有张量语义的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class TensorAttribute : Attribute
{
}