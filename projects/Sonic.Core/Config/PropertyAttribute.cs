using System;

namespace Core.Config;

/// <summary>
///     标记配置属性的自定义键名，覆盖默认的命名约定转换。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PropertyAttribute : Attribute
{
    /// <summary>
    ///     初始化配置属性特性。
    /// </summary>
    /// <param name="name">自定义配置键名。</param>
    public PropertyAttribute(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     自定义配置键名。
    /// </summary>
    public string name { get; }
}