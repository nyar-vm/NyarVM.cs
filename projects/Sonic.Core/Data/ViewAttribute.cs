using System;

namespace Core.Data;

/// <summary>
///     标记视图模型，控制反序列化和架构的自动生成。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ViewAttribute : Attribute
{
    /// <summary>
    ///     是否生成反序列化器。
    /// </summary>
    public bool generate_deserializer { get; set; } = true;

    /// <summary>
    ///     是否生成架构定义。
    /// </summary>
    public bool generate_schema { get; set; } = false;
}