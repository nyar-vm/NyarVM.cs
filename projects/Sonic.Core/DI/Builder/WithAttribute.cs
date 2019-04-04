using System;

namespace Core.DI.Builder;

/// <summary>
///     标记字段或属性以在生成的构建器中创建对应的 With 方法
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class WithAttribute : Attribute
{
    /// <summary>
    ///     自定义 With 方法名称，为 null 时根据成员名称自动生成
    /// </summary>
    public string? method_name { get; set; }
}