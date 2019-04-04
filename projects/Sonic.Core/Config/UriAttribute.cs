using System;

namespace Core.Config;

/// <summary>
///     验证配置属性值为有效的 URI 格式。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UriAttribute : Attribute
{
}