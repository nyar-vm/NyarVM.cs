using System;

namespace Core.Config;

/// <summary>
///     标记配置属性为必填项，配置加载时验证非空。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredAttribute : Attribute
{
}