using System;

namespace Core.DI;

/// <summary>
///     声明式属性注入标记，用于标记系统类的属性为依赖注入目标。
///     框架在创建系统实例后，从 DI 容器解析对应服务并设置属性值。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class WireAttribute : Attribute
{
}