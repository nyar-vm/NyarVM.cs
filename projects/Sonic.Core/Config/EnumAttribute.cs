using System;

namespace Core.Config;

/// <summary>
///     验证配置属性值是否为指定枚举类型的有效成员。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EnumAttribute : Attribute
{
}