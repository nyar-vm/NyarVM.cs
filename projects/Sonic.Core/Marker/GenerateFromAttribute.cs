using System;

namespace Core.Marker;

/// <summary>
///     通用属性，指示 Source Generator 从外部模板或定义生成代码。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GenerateFromAttribute : Attribute
{
    /// <summary>
    ///     初始化从指定定义类型生成代码的特性。
    /// </summary>
    /// <param name="definitionType">定义类型。</param>
    public GenerateFromAttribute(Type definitionType)
    {
        definition_type = definitionType;
    }

    /// <summary>
    ///     获取定义类型。
    /// </summary>
    public Type definition_type { get; }
}