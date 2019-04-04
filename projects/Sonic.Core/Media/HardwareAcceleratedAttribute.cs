using System;

namespace Core.Media;

/// <summary>
///     标记类型或方法使用硬件加速。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class HardwareAcceleratedAttribute : Attribute
{
}